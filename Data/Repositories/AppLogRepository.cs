using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Data;

namespace WikiScrapper.Data.Repositories;

/// <summary>EF Core implementation of <see cref="IAppLogRepository"/>.</summary>
public class AppLogRepository(AppDbContext dbContext) : IAppLogRepository
{
    /// <inheritdoc />
    public Task AddAsync(AppLog entry, CancellationToken cancellationToken = default)
    {
        dbContext.AppLogs.Add(entry);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppLog>> GetRecentAsync(
        int count,
        AppLogLevel? level = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AppLogs.AsQueryable();

        if (level is not null)
        {
            query = query.Where(l => l.Level == level);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Take(Math.Clamp(count, 1, 500))
            .ToListAsync(cancellationToken);
    }
}
