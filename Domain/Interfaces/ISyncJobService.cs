using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Starts Wikipedia synchronization as a background job and exposes live status.
/// Prevents overlapping runs.
/// </summary>
public interface ISyncJobService
{
    /// <summary>
    /// Starts a background synchronization if none is running.
    /// </summary>
    /// <returns><c>true</c> if this call started a new run; <c>false</c> if one is already in progress.</returns>
    bool TryStart();

    /// <summary>Returns a snapshot of the current or last-completed run.</summary>
    SyncStatusDto GetStatus();
}
