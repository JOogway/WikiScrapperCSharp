namespace WikiScrapper.Domain.Dtos;

/// <summary>
/// Outcome of a full data synchronization run (voivodeships + countries).
/// </summary>
public record SyncResult
{
    /// <summary>Number of items whose description was fetched and saved.</summary>
    public int Succeeded { get; init; }

    /// <summary>Number of items that threw an error while fetching or saving.</summary>
    public int Failed { get; init; }

    /// <summary>Number of items skipped because Wikipedia returned no usable summary (missing page or empty extract).</summary>
    public int Skipped { get; init; }

    /// <summary>Per-item error/skip messages collected during the run.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>UTC timestamp of when the run started.</summary>
    public DateTime StartedAtUtc { get; init; }

    /// <summary>UTC timestamp of when the run completed.</summary>
    public DateTime CompletedAtUtc { get; init; }

    /// <summary>Total wall-clock duration of the run.</summary>
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

    /// <summary>Total number of items processed.</summary>
    public int Total => Succeeded + Failed + Skipped;
}
