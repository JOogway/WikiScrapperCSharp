using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Data;

namespace WikiScrapper.Data.Repositories;

/// <summary>EF Core implementation of <see cref="ICountryRepository"/>.</summary>
public class CountryRepository(AppDbContext dbContext) : ICountryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Countries
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<CountryDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        bool? fetched = null,
        CountrySortColumn sort = CountrySortColumn.Name,
        SortDirection dir = SortDirection.Asc,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = ApplyFilters(dbContext.Countries.AsQueryable(), search, fetched, language);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectToDto(ApplySort(query, sort, dir, language), language)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CountryDto>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Projects only the requested language's columns, so list pages never pull
    /// the other language's potentially large description text from the database.
    /// </summary>
    private static IQueryable<CountryDto> ProjectToDto(IQueryable<Country> query, WikiLanguage language) =>
        language == WikiLanguage.Pl
            ? query.Select(c => new CountryDto(c.Id, c.Name, c.Code, c.WikiTitlePl, c.WikiUrlPl, c.DescriptionPl, c.FetchedAtPl))
            : query.Select(c => new CountryDto(c.Id, c.Name, c.Code, c.WikiTitle, c.WikiUrl, c.Description, c.FetchedAt));

    /// <inheritdoc />
    public Task<int> CountFilteredAsync(
        string? search,
        bool? fetched = null,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default) =>
        ApplyFilters(dbContext.Countries.AsQueryable(), search, fetched, language)
            .CountAsync(cancellationToken);

    private static IQueryable<Country> ApplyFilters(
        IQueryable<Country> query,
        string? search,
        bool? fetched,
        WikiLanguage language)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Code.Contains(term));
        }

        if (fetched == true)
        {
            query = language == WikiLanguage.Pl
                ? query.Where(c => c.DescriptionPl != null)
                : query.Where(c => c.Description != null);
        }
        else if (fetched == false)
        {
            query = language == WikiLanguage.Pl
                ? query.Where(c => c.DescriptionPl == null)
                : query.Where(c => c.Description == null);
        }

        return query;
    }

    private static IOrderedQueryable<Country> ApplySort(
        IQueryable<Country> query,
        CountrySortColumn sort,
        SortDirection dir,
        WikiLanguage language)
    {
        var descending = dir == SortDirection.Desc;

        if (sort == CountrySortColumn.Fetched)
        {
            return descending
                ? query.OrderBy(c => language == WikiLanguage.Pl ? c.FetchedAtPl == null : c.FetchedAt == null)
                    .ThenByDescending(c => language == WikiLanguage.Pl ? c.FetchedAtPl : c.FetchedAt)
                    .ThenBy(c => c.Name)
                : query.OrderBy(c => language == WikiLanguage.Pl ? c.FetchedAtPl == null : c.FetchedAt == null)
                    .ThenBy(c => language == WikiLanguage.Pl ? c.FetchedAtPl : c.FetchedAt)
                    .ThenBy(c => c.Name);
        }

        return sort switch
        {
            CountrySortColumn.Code when descending => query.OrderByDescending(c => c.Code).ThenBy(c => c.Name),
            CountrySortColumn.Code => query.OrderBy(c => c.Code).ThenBy(c => c.Name),
            _ when descending => query.OrderByDescending(c => c.Name),
            _ => query.OrderBy(c => c.Name),
        };
    }

    /// <inheritdoc />
    public Task<Country?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<FetchStats> GetFetchStatsAsync(WikiLanguage language, CancellationToken cancellationToken = default)
    {
        // Single aggregate query (COUNT + conditional COUNT + MAX) instead of three round-trips.
        var stats = language == WikiLanguage.Pl
            ? await dbContext.Countries
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Fetched = g.Count(c => c.DescriptionPl != null),
                    LastFetchedAt = g.Max(c => c.FetchedAtPl),
                })
                .FirstOrDefaultAsync(cancellationToken)
            : await dbContext.Countries
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Fetched = g.Count(c => c.Description != null),
                    LastFetchedAt = g.Max(c => c.FetchedAt),
                })
                .FirstOrDefaultAsync(cancellationToken);

        return stats is null
            ? new FetchStats(0, 0, null)
            : new FetchStats(stats.Total, stats.Fetched, stats.LastFetchedAt);
    }
}
