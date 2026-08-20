using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Buffers entity updates during a sync run and persists them in batches,
/// so a full run issues a handful of commits instead of one per Wikipedia page.
/// Implementations are not thread-safe; callers must serialize access
/// (the sync service already guards all database work with a single gate).
/// </summary>
public interface ISyncDbBatch
{
    /// <summary>
    /// Marks the entity as updated and flushes automatically once the
    /// internal batch size is reached.
    /// </summary>
    Task QueueUpdateAsync(Entity entity, CancellationToken cancellationToken = default);

    /// <summary>Persists any buffered updates. Safe to call when nothing is pending.</summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
