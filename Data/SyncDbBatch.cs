using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Data;

/// <summary>
/// EF Core implementation of <see cref="ISyncDbBatch"/>: queued entities are marked
/// modified on the scoped <see cref="AppDbContext"/> and saved together every
/// <see cref="BatchSize"/> updates (plus a final explicit flush at the end of a run).
/// If a flush fails the pending counter is kept, so the updates are retried on the
/// next flush instead of being lost.
/// </summary>
public class SyncDbBatch(AppDbContext dbContext) : ISyncDbBatch
{
    /// <summary>Number of queued updates that triggers an automatic flush.</summary>
    public const int BatchSize = 25;

    private int _pending;

    /// <inheritdoc />
    public async Task QueueUpdateAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Update(entity);
        _pending++;

        if (_pending >= BatchSize)
        {
            await FlushAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_pending == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _pending = 0;
    }
}
