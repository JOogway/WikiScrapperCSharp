using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Persistence operations for <see cref="AppLog"/> audit entries.
/// </summary>
public interface IAppLogRepository
{
    /// <summary>Appends a new audit entry.</summary>
    Task AddAsync(AppLog entry, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent audit entries, newest first, optionally filtered by level.</summary>
    Task<IReadOnlyList<AppLog>> GetRecentAsync(
        int count,
        AppLogLevel? level = null,
        CancellationToken cancellationToken = default);
}
