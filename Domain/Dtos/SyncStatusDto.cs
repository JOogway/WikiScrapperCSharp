namespace WikiScrapper.Domain.Dtos;

/// <summary>
/// Live or last-completed status of a Wikipedia synchronization job.
/// </summary>
public record SyncStatusDto
{
    /// <summary>Whether a synchronization is currently running.</summary>
    public bool IsRunning { get; init; }

    /// <summary>Items processed so far in the current (or last) run.</summary>
    public int Processed { get; init; }

    /// <summary>Total items in the current (or last) run.</summary>
    public int Total { get; init; }

    /// <summary>Successful fetches in the current (or last) run.</summary>
    public int Succeeded { get; init; }

    /// <summary>Failed fetches in the current (or last) run.</summary>
    public int Failed { get; init; }

    /// <summary>Items skipped because Wikipedia returned no summary.</summary>
    public int Skipped { get; init; }

    /// <summary>Name of the item currently being fetched, if a run is in progress.</summary>
    public string? CurrentItem { get; init; }

    /// <summary>UTC start of the current (or last) run.</summary>
    public DateTime? StartedAtUtc { get; init; }

    /// <summary>UTC completion of the last finished run. Null while running.</summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>Per-item error/skip messages from the current (or last) run.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Completion percentage, or null when total is unknown.</summary>
    public double? Percent => Total <= 0 ? null : Math.Round(100.0 * Processed / Total, 1);
}
