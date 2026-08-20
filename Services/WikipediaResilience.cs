using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace WikiScrapper.Services;

/// <summary>
/// Wikipedia REST API resilience: eight workers run at full speed; only the worker
/// that receives HTTP 429 waits and retries. The circuit breaker does not treat
/// rate limiting as an outage, so the other workers keep fetching.
/// </summary>
public static class WikipediaResilience
{
    public const int MaxRetryAttempts = 5;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan HttpClientTimeout = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Configures the HTTP resilience pipeline (outermost to innermost):
    /// retry → circuit breaker → per-attempt timeout.
    /// </summary>
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder, ILogger logger)
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            // DelayGenerator below replaces Delay/BackoffType; Retry-After is honored but capped
            // so a single item cannot stall the run for minutes. The wait applies only to that
            // request's worker; Parallel.ForEachAsync keeps the other workers busy.
            ShouldRetryAfterHeader = false,
            DelayGenerator = args => new ValueTask<TimeSpan?>(ResolveRetryDelay(args)),
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Wikipedia request will be retried after {RetryDelay} (attempt {Attempt}/{MaxAttempts}, status {StatusCode})",
                    args.RetryDelay,
                    args.AttemptNumber + 1,
                    MaxRetryAttempts,
                    args.Outcome.Result?.StatusCode);
                return default;
            },
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            // 429 means "slow down this request", not "the API is down". Opening the circuit would
            // fail the remaining countries immediately instead of waiting on that worker only.
            ShouldHandle = static args =>
            {
                if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests })
                {
                    return PredicateResult.False();
                }

                return new ValueTask<bool>(HttpClientResiliencePredicates.IsTransient(args.Outcome));
            },
        });

        builder.AddTimeout(AttemptTimeout);
    }

    /// <summary>
    /// Prefers a capped <c>Retry-After</c> header; otherwise uses exponential backoff.
    /// </summary>
    public static TimeSpan ResolveRetryDelay(RetryDelayGeneratorArguments<HttpResponseMessage> args)
    {
        if (TryGetRetryAfter(args.Outcome.Result, out var retryAfter) && retryAfter > TimeSpan.Zero)
        {
            return Cap(retryAfter);
        }

        var exponential = TimeSpan.FromSeconds(2 * Math.Pow(2, args.AttemptNumber));
        return Cap(exponential);
    }

    private static bool TryGetRetryAfter(HttpResponseMessage? response, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        var header = response?.Headers.RetryAfter;
        if (header is null)
        {
            return false;
        }

        if (header.Delta is { } delta)
        {
            delay = delta;
            return true;
        }

        if (header.Date is { } date)
        {
            delay = date - DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }

    private static TimeSpan Cap(TimeSpan delay) =>
        delay > MaxRetryDelay ? MaxRetryDelay : delay;

    /// <summary>
    /// Wikipedia HTTP 429, including the wrapped form that surfaces after Polly retries
    /// are exhausted.
    /// </summary>
    public static bool IsRateLimited(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests })
            {
                return true;
            }

            if (current.Message.Contains("429", StringComparison.Ordinal)
                && current.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
