using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Data;

/// <summary>
/// EF Core database context for the application.
/// Automatically maintains <see cref="Entity.CreatedAt"/> and <see cref="Entity.UpdatedAt"/> on save.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>The 16 Polish voivodeships.</summary>
    public DbSet<Voivodeship> Voivodeships => Set<Voivodeship>();

    /// <summary>World countries.</summary>
    public DbSet<Country> Countries => Set<Country>();

    /// <summary>Application audit log entries.</summary>
    public DbSet<AppLog> AppLogs => Set<AppLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
