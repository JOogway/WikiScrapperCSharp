using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Services;

/// <summary>
/// Orchestrates a full synchronization run: fetches Wikipedia summaries for every
/// voivodeship and country and persists the descriptions. Wikipedia HTTP calls run
/// concurrently (bounded by <see cref="WikipediaClientOptions.MaxConcurrency"/>).
/// A worker that receives HTTP 429 waits and retries; the other workers keep fetching.
/// Database writes are serialized because EF Core's <c>DbContext</c> is not thread-safe,
/// and are committed in batches via <see cref="ISyncDbBatch"/> instead of one commit per page.
/// A failure on one item is recorded (log file + <see cref="AppLog"/> audit table)
/// and the run continues.
/// </summary>
public class DataSyncService(
    IVoivodeshipRepository voivodeshipRepository,
    ICountryRepository countryRepository,
    IWikipediaService wikipediaService,
    IAppLogRepository appLogRepository,
    ISyncDbBatch syncBatch,
    ILogger<DataSyncService> logger,
    ISyncProgress? progress = null,
    IOptions<WikipediaClientOptions>? wikipediaOptions = null,
    int? maxConcurrency = null,
    TimeSpan? rateLimitRetryDelay = null,
    int? rateLimitRetryAttempts = null) : IDataSyncService
{
    private const string LogSource = "DataSync";
    private const int DefaultRateLimitRetryAttempts = 3;

    private readonly int _maxConcurrency = Math.Clamp(
        maxConcurrency ?? wikipediaOptions?.Value.MaxConcurrency ?? WikipediaClientOptions.DefaultMaxConcurrency,
        1,
        WikipediaClientOptions.AbsoluteMaxConcurrency);

    /// <summary>
    /// Extra attempts at the orchestration layer after the HTTP pipeline has already
    /// retried. Tests pass <see cref="TimeSpan.Zero"/> so they do not wait.
    /// </summary>
    private readonly TimeSpan _rateLimitRetryDelay = rateLimitRetryDelay ?? TimeSpan.FromSeconds(4);

    private readonly int _rateLimitRetryAttempts = Math.Clamp(
        rateLimitRetryAttempts ?? DefaultRateLimitRetryAttempts,
        1,
        6);

    /// <inheritdoc />
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var succeeded = 0;
        var failed = 0;
        var skipped = 0;
        var errors = new ConcurrentQueue<string>();
        using var dbGate = new SemaphoreSlim(1, 1);

        logger.LogInformation(
            "Data synchronization started with max concurrency {MaxConcurrency}",
            _maxConcurrency);
        await AuditAsync(AppLogLevel.Information, "Data synchronization started", null, dbGate, cancellationToken);

        var voivodeships = await voivodeshipRepository.GetAllAsync(cancellationToken);
        var countries = await countryRepository.GetAllAsync(cancellationToken);
        var workItems = BuildWorkItems(voivodeships, countries, dbGate);
        progress?.Begin(workItems.Count);

        await Parallel.ForEachAsync(
            workItems,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxConcurrency,
                CancellationToken = cancellationToken,
            },
            async (item, ct) =>
            {
                progress?.ItemStarted(item.Name);
                var outcome = await SyncItemAsync(item, errors, dbGate, ct);
                Tally(outcome, ref succeeded, ref failed, ref skipped);
                progress?.ItemFinished(outcome == ItemOutcome.Succeeded, outcome == ItemOutcome.Skipped);
            });

        // Persist whatever the last partial batch still holds before reporting completion.
        await dbGate.WaitAsync(cancellationToken);
        try
        {
            await syncBatch.FlushAsync(cancellationToken);
        }
        finally
        {
            dbGate.Release();
        }

        var result = new SyncResult
        {
            Succeeded = succeeded,
            Failed = failed,
            Skipped = skipped,
            Errors = [.. errors],
            StartedAtUtc = startedAt,
            CompletedAtUtc = DateTime.UtcNow,
        };

        logger.LogInformation(
            "Data synchronization finished: {Succeeded} succeeded, {Failed} failed, {Skipped} skipped in {Duration}",
            result.Succeeded, result.Failed, result.Skipped, result.Duration);

        await AuditAsync(
            result.Failed > 0 ? AppLogLevel.Warning : AppLogLevel.Information,
            $"Data synchronization finished: {result.Succeeded} succeeded, {result.Failed} failed, " +
            $"{result.Skipped} skipped in {result.Duration:mm\\:ss}.",
            null,
            dbGate,
            cancellationToken);

        progress?.Complete(result);
        return result;
    }

    private List<WorkItem> BuildWorkItems(
        IReadOnlyList<Voivodeship> voivodeships,
        IReadOnlyList<Country> countries,
        SemaphoreSlim dbGate)
    {
        var items = new List<WorkItem>((voivodeships.Count + countries.Count) * 2);
        AddWorkItems(items, voivodeships, WikiLanguage.En, v => (v.Name, v.WikiTitle), dbGate);
        AddWorkItems(items, voivodeships, WikiLanguage.Pl, v => (v.Name, v.WikiTitlePl), dbGate);
        AddWorkItems(items, countries, WikiLanguage.En, c => (c.Name, c.WikiTitle), dbGate);
        AddWorkItems(items, countries, WikiLanguage.Pl, c => (c.Name, c.WikiTitlePl), dbGate);
        return items;
    }

    private void AddWorkItems<T>(
        List<WorkItem> items,
        IEnumerable<T> entities,
        WikiLanguage language,
        Func<T, (string Name, string WikiTitle)> selector,
        SemaphoreSlim dbGate)
        where T : Entity, ILocalizedWikiEntity
    {
        foreach (var entity in entities)
        {
            var (name, wikiTitle) = selector(entity);
            items.Add(new WorkItem(
                name,
                wikiTitle,
                language,
                async (summary, ct) =>
                {
                    await dbGate.WaitAsync(ct);
                    try
                    {
                        entity.ApplySummary(summary, language);
                        await syncBatch.QueueUpdateAsync(entity, ct);
                    }
                    finally
                    {
                        dbGate.Release();
                    }
                }));
        }
    }

    private enum ItemOutcome { Succeeded, Failed, Skipped }

    private sealed record WorkItem(
        string Name,
        string WikiTitle,
        WikiLanguage Language,
        Func<WikiPageSummary, CancellationToken, Task> PersistAsync);

    private async Task<ItemOutcome> SyncItemAsync(
        WorkItem item,
        ConcurrentQueue<string> errors,
        SemaphoreSlim dbGate,
        CancellationToken cancellationToken)
    {
        Exception? lastRateLimit = null;

        for (var attempt = 1; attempt <= _rateLimitRetryAttempts; attempt++)
        {
            try
            {
                var summary = await wikipediaService.GetPageSummaryAsync(item.WikiTitle, item.Language, cancellationToken);

                if (summary is null)
                {
                    var skipMessage =
                        $"No Wikipedia summary available for '{item.Name}' ({item.Language}, page: '{item.WikiTitle}').";
                    errors.Enqueue(skipMessage);
                    logger.LogWarning("Skipped {Item}: no summary for page {WikiTitle}", item.Name, item.WikiTitle);
                    await AuditAsync(AppLogLevel.Warning, skipMessage, null, dbGate, cancellationToken);
                    return ItemOutcome.Skipped;
                }

                await item.PersistAsync(summary, cancellationToken);
                logger.LogDebug("Synced {Item} from page {WikiTitle}", item.Name, item.WikiTitle);
                return ItemOutcome.Succeeded;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Only caller-requested cancellation aborts the run. HTTP timeouts also surface
                // as TaskCanceledException and must be treated as a per-item failure instead.
                throw;
            }
            catch (Exception ex) when (WikipediaResilience.IsRateLimited(ex) && attempt < _rateLimitRetryAttempts)
            {
                lastRateLimit = ex;
                var wait = ResolveItemRetryDelay(attempt);
                logger.LogWarning(
                    ex,
                    "Rate-limited while syncing {Item} (page {WikiTitle}); waiting {Delay} before retry {Attempt}/{Total}",
                    item.Name, item.WikiTitle, wait, attempt, _rateLimitRetryAttempts);
                await Task.Delay(wait, cancellationToken);
            }
            catch (Exception ex)
            {
                return await FailItemAsync(item, errors, dbGate, ex, cancellationToken);
            }
        }

        return await FailItemAsync(item, errors, dbGate, lastRateLimit!, cancellationToken);
    }

    private TimeSpan ResolveItemRetryDelay(int attempt)
    {
        if (_rateLimitRetryDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var delay = TimeSpan.FromSeconds(_rateLimitRetryDelay.TotalSeconds * Math.Pow(2, attempt - 1));
        return delay > WikipediaResilience.MaxRetryDelay ? WikipediaResilience.MaxRetryDelay : delay;
    }

    private async Task<ItemOutcome> FailItemAsync(
        WorkItem item,
        ConcurrentQueue<string> errors,
        SemaphoreSlim dbGate,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var message = $"Failed to sync '{item.Name}' (page: '{item.WikiTitle}'): {ex.Message}";
        errors.Enqueue(message);
        logger.LogError(ex, "Failed to sync {Item} from page {WikiTitle}", item.Name, item.WikiTitle);
        await AuditAsync(AppLogLevel.Error, message, ex.ToString(), dbGate, cancellationToken);
        return ItemOutcome.Failed;
    }

    private static void Tally(ItemOutcome outcome, ref int succeeded, ref int failed, ref int skipped)
    {
        switch (outcome)
        {
            case ItemOutcome.Succeeded: Interlocked.Increment(ref succeeded); break;
            case ItemOutcome.Failed: Interlocked.Increment(ref failed); break;
            case ItemOutcome.Skipped: Interlocked.Increment(ref skipped); break;
        }
    }

    private async Task AuditAsync(
        AppLogLevel level,
        string message,
        string? exception,
        SemaphoreSlim dbGate,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbGate.WaitAsync(cancellationToken);
            try
            {
                await appLogRepository.AddAsync(
                    new AppLog { Level = level, Message = message, Source = LogSource, Exception = exception },
                    cancellationToken);
            }
            finally
            {
                dbGate.Release();
            }
        }
        catch (Exception ex)
        {
            // The audit table must never take down a sync run; the file log still captures everything.
            logger.LogError(ex, "Failed to write audit entry: {Message}", message);
        }
    }
}
