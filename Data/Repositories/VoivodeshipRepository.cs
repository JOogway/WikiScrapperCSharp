using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Data;

namespace WikiScrapper.Data.Repositories;

/// <summary>EF Core implementation of <see cref="IVoivodeshipRepository"/>.</summary>
public class VoivodeshipRepository(AppDbContext dbContext) : IVoivodeshipRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Voivodeship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Voivodeships
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VoivodeshipDto>> GetListAsync(WikiLanguage language, CancellationToken cancellationToken = default) =>
        language == WikiLanguage.Pl
            ? await dbContext.Voivodeships
                .OrderBy(v => v.Name)
                .Select(v => new VoivodeshipDto(v.Id, v.Name, v.WikiTitlePl, v.WikiUrlPl, v.DescriptionPl, v.FetchedAtPl))
                .ToListAsync(cancellationToken)
            : await dbContext.Voivodeships
                .OrderBy(v => v.Name)
                .Select(v => new VoivodeshipDto(v.Id, v.Name, v.WikiTitle, v.WikiUrl, v.Description, v.FetchedAt))
                .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Voivodeship?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Voivodeships.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<FetchStats> GetFetchStatsAsync(WikiLanguage language, CancellationToken cancellationToken = default)
    {
        // Single aggregate query (COUNT + conditional COUNT + MAX) instead of three round-trips.
        var stats = language == WikiLanguage.Pl
            ? await dbContext.Voivodeships
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Fetched = g.Count(v => v.DescriptionPl != null),
                    LastFetchedAt = g.Max(v => v.FetchedAtPl),
                })
                .FirstOrDefaultAsync(cancellationToken)
            : await dbContext.Voivodeships
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Fetched = g.Count(v => v.Description != null),
                    LastFetchedAt = g.Max(v => v.FetchedAt),
                })
                .FirstOrDefaultAsync(cancellationToken);

        return stats is null
            ? new FetchStats(0, 0, null)
            : new FetchStats(stats.Total, stats.Fetched, stats.LastFetchedAt);
    }
}
