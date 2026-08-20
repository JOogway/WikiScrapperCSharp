using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WikiScrapper.Services;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Tests.Services;

/// <summary>Unit tests for the in-process background sync job.</summary>
public class SyncJobServiceTests
{
    [Fact]
    public async Task TryStart_RejectsSecondConcurrentRun()
    {
        var gate = new TaskCompletionSource<SyncResult>();
        var sync = Substitute.For<IDataSyncService>();
        sync.SyncAllAsync(Arg.Any<CancellationToken>()).Returns(_ => gate.Task);

        var services = new ServiceCollection();
        services.AddSingleton(sync);
        await using var provider = services.BuildServiceProvider();

        var job = new SyncJobService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SyncJobService>.Instance);

        job.TryStart().Should().BeTrue();
        job.TryStart().Should().BeFalse();
        job.GetStatus().IsRunning.Should().BeTrue();

        gate.SetResult(new SyncResult
        {
            Succeeded = 1,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
        });

        await WaitUntil(() => !job.GetStatus().IsRunning);
        job.GetStatus().IsRunning.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_ReportsLiveProgress()
    {
        var job = new SyncJobService(
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<SyncJobService>.Instance);

        job.Begin(10);
        job.ItemStarted("Poland");
        job.ItemFinished(succeeded: true, skipped: false);

        var status = job.GetStatus();
        status.Total.Should().Be(10);
        status.Processed.Should().Be(1);
        status.Succeeded.Should().Be(1);
        status.CurrentItem.Should().Be("Poland");
        status.Percent.Should().Be(10);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }
    }
}
