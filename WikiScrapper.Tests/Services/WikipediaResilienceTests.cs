using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Polly;
using Polly.Retry;
using WikiScrapper.Services;

namespace WikiScrapper.Tests.Services;

public class WikipediaResilienceTests
{
    [Fact]
    public void ResolveRetryDelay_CapsLongRetryAfterHeader()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(5));

        var delay = Resolve(response, attemptNumber: 0);

        delay.Should().Be(WikipediaResilience.MaxRetryDelay);
    }

    [Fact]
    public void ResolveRetryDelay_HonorsShortRetryAfterHeader()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3));

        var delay = Resolve(response, attemptNumber: 0);

        delay.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void ResolveRetryDelay_UsesExponentialBackoff_WhenRetryAfterIsMissing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        Resolve(response, attemptNumber: 0).Should().Be(TimeSpan.FromSeconds(2));
        Resolve(response, attemptNumber: 1).Should().Be(TimeSpan.FromSeconds(4));
        Resolve(response, attemptNumber: 4).Should().Be(WikipediaResilience.MaxRetryDelay);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsRateLimited_Detects429(bool setStatusCode)
    {
        Exception ex = setStatusCode
            ? new HttpRequestException("429 Too Many Requests", null, HttpStatusCode.TooManyRequests)
            : new HttpRequestException("Response status code does not indicate success: 429 (Too Many Requests).");

        WikipediaResilience.IsRateLimited(ex).Should().BeTrue();
        WikipediaResilience.IsRateLimited(new HttpRequestException("500 Internal Server Error")).Should().BeFalse();
    }

    private static TimeSpan Resolve(HttpResponseMessage response, int attemptNumber)
    {
        var context = ResilienceContextPool.Shared.Get();
        try
        {
            var args = new RetryDelayGeneratorArguments<HttpResponseMessage>(
                context,
                Outcome.FromResult(response),
                attemptNumber);
            return WikipediaResilience.ResolveRetryDelay(args);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }
}
