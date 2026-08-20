using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Services;

/// <summary>
/// In-process Wikipedia sync job: at most one run at a time, with a thread-safe
/// status snapshot that the UI can poll. The HTTP request that starts the job
/// returns immediately; work continues on a background task with its own DI scope.
/// </summary>
public sealed class SyncJobService(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncJobService> logger) : ISyncJobService, ISyncProgress
{
    private readonly object _gate = new();
    private int _runningFlag;
    private bool _isRunning;
    private int _processed;
    private int _total;
    private int _succeeded;
    private int _failed;
    private int _skipped;
    private string? _currentItem;
    private DateTime? _startedAtUtc;
    private DateTime? _completedAtUtc;
    private List<string> _errors = [];

    /// <inheritdoc />
    public bool TryStart()
    {
        if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) != 0)
        {
            return false;
        }

        lock (_gate)
        {
            _isRunning = true;
            _processed = 0;
            _total = 0;
            _succeeded = 0;
            _failed = 0;
            _skipped = 0;
            _currentItem = null;
            _startedAtUtc = DateTime.UtcNow;
            _completedAtUtc = null;
            _errors = [];
        }

        _ = Task.Run(RunAsync);
        return true;
    }

    /// <inheritdoc />
    public SyncStatusDto GetStatus()
    {
        lock (_gate)
        {
            return new SyncStatusDto
            {
                IsRunning = _isRunning || Volatile.Read(ref _runningFlag) == 1,
                Processed = _processed,
                Total = _total,
                Succeeded = _succeeded,
                Failed = _failed,
                Skipped = _skipped,
                CurrentItem = _currentItem,
                StartedAtUtc = _startedAtUtc,
                CompletedAtUtc = _completedAtUtc,
                Errors = [.. _errors],
            };
        }
    }

    /// <inheritdoc />
    public void Begin(int total)
    {
        lock (_gate)
        {
            _total = total;
        }
    }

    /// <inheritdoc />
    public void ItemStarted(string itemName)
    {
        lock (_gate)
        {
            _currentItem = itemName;
        }
    }

    /// <inheritdoc />
    public void ItemFinished(bool succeeded, bool skipped)
    {
        lock (_gate)
        {
            _processed++;
            if (succeeded)
            {
                _succeeded++;
            }
            else if (skipped)
            {
                _skipped++;
            }
            else
            {
                _failed++;
            }
        }
    }

    /// <inheritdoc />
    public void Complete(SyncResult result)
    {
        lock (_gate)
        {
            _isRunning = false;
            _currentItem = null;
            _succeeded = result.Succeeded;
            _failed = result.Failed;
            _skipped = result.Skipped;
            _processed = result.Total;
            _total = Math.Max(_total, result.Total);
            _errors = [.. result.Errors];
            _completedAtUtc = result.CompletedAtUtc;
        }
    }

    private async Task RunAsync()
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sync = scope.ServiceProvider.GetRequiredService<IDataSyncService>();
            // Do not pass a request CancellationToken: the starter HTTP call returns immediately.
            await sync.SyncAllAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background synchronization failed");
            lock (_gate)
            {
                _isRunning = false;
                _currentItem = null;
                _completedAtUtc = DateTime.UtcNow;
                _errors.Add($"Synchronization aborted: {ex.Message}");
            }
        }
        finally
        {
            lock (_gate)
            {
                _isRunning = false;
                _currentItem = null;
            }

            Interlocked.Exchange(ref _runningFlag, 0);
        }
    }
}
