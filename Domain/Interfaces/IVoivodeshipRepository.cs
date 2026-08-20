using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Persistence operations for <see cref="Voivodeship"/> entities.
/// </summary>
public interface IVoivodeshipRepository
{
    /// <summary>Returns all voivodeships ordered by name, as full entities (used by sync).</summary>
    Task<IReadOnlyList<Voivodeship>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all voivodeships ordered by name, projected to <see cref="VoivodeshipDto"/> in SQL
    /// so only the requested language's description column is read.
    /// </summary>
    Task<IReadOnlyList<VoivodeshipDto>> GetListAsync(WikiLanguage language, CancellationToken cancellationToken = default);

    /// <summary>Returns a single voivodeship or null when not found.</summary>
    Task<Voivodeship?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregate fetch counts and the latest fetch timestamp for a language.</summary>
    Task<FetchStats> GetFetchStatsAsync(WikiLanguage language, CancellationToken cancellationToken = default);
}
