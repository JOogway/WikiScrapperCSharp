using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Receives live progress from <see cref="IDataSyncService"/> during a run.
/// </summary>
public interface ISyncProgress
{
    /// <summary>Called once the total number of items is known.</summary>
    void Begin(int total);

    /// <summary>Called just before an item is fetched.</summary>
    void ItemStarted(string itemName);

    /// <summary>Called after an item has been fetched, skipped, or failed.</summary>
    void ItemFinished(bool succeeded, bool skipped);

    /// <summary>Called when the run finishes normally.</summary>
    void Complete(SyncResult result);
}
