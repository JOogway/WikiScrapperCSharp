using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Orchestrates fetching Wikipedia descriptions and persisting them to the database.
/// </summary>
public interface IDataSyncService
{
    /// <summary>
    /// Synchronizes descriptions for all voivodeships and countries.
    /// Individual item failures are recorded and do not abort the run.
    /// </summary>
    Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default);
}
