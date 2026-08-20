using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Persistence operations for <see cref="Country"/> entities.
/// </summary>
public interface ICountryRepository
{
    /// <summary>Returns all countries ordered by name.</summary>
    Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of countries, optionally filtered by a case-insensitive
    /// substring match on name or ISO code, and by whether a description has been fetched.
    /// Sort columns: <c>name</c> (default), <c>code</c>, <c>fetched</c>.
    /// Rows are projected to <see cref="CountryDto"/> in SQL so only the requested
    /// language's description column is read.
    /// </summary>
    Task<PagedResult<CountryDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        bool? fetched = null,
        CountrySortColumn sort = CountrySortColumn.Name,
        SortDirection dir = SortDirection.Asc,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default);

    /// <summary>Returns how many countries match the current filters (no row materialization).</summary>
    Task<int> CountFilteredAsync(
        string? search,
        bool? fetched = null,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single country or null when not found.</summary>
    Task<Country?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregate fetch counts and the latest fetch timestamp for a language.</summary>
    Task<FetchStats> GetFetchStatsAsync(WikiLanguage language, CancellationToken cancellationToken = default);
}
