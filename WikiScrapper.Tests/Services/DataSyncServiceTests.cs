using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WikiScrapper.Domain;
using WikiScrapper.Services;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DataSyncService"/> with mocked repositories and Wikipedia service.
/// </summary>
public class DataSyncServiceTests
{
    private readonly IVoivodeshipRepository _voivodeshipRepository = Substitute.For<IVoivodeshipRepository>();
    private readonly ICountryRepository _countryRepository = Substitute.For<ICountryRepository>();
    private readonly IWikipediaService _wikipediaService = Substitute.For<IWikipediaService>();
    private readonly IAppLogRepository _appLogRepository = Substitute.For<IAppLogRepository>();
    private readonly ISyncDbBatch _syncBatch = Substitute.For<ISyncDbBatch>();

    private DataSyncService CreateService(int maxConcurrency = 8) => new(
        _voivodeshipRepository,
        _countryRepository,
        _wikipediaService,
        _appLogRepository,
        _syncBatch,
        NullLogger<DataSyncService>.Instance,
        maxConcurrency: maxConcurrency,
        rateLimitRetryDelay: TimeSpan.Zero);

    private static Voivodeship MakeVoivodeship(string name, string wikiTitle) =>
        new() { Name = name, WikiTitle = wikiTitle, WikiTitlePl = name };

    private static Country MakeCountry(string name, string code) =>
        new() { Name = name, Code = code, WikiTitle = name, WikiTitlePl = name };

    private static WikiPageSummary MakeSummary(string title) =>
        new(title, $"{title} description.", $"https://en.wikipedia.org/wiki/{title}");

    [Fact]
    public async Task SyncAllAsync_UpdatesAllItems_WhenWikipediaSucceeds()
    {
        var voivodeship = MakeVoivodeship("Województwo mazowieckie", "Masovian Voivodeship");
        var country = MakeCountry("Poland", "PL");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([voivodeship]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(call => MakeSummary(call.Arg<string>()));

        var result = await CreateService().SyncAllAsync();

        result.Succeeded.Should().Be(4);
        result.Failed.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Total.Should().Be(4);

        voivodeship.Description.Should().Be("Masovian Voivodeship description.");
        voivodeship.DescriptionPl.Should().Be("Województwo mazowieckie description.");
        voivodeship.FetchedAt.Should().NotBeNull();
        voivodeship.FetchedAtPl.Should().NotBeNull();
        country.Description.Should().Be("Poland description.");
        country.DescriptionPl.Should().Be("Poland description.");
        country.WikiUrl.Should().Be("https://en.wikipedia.org/wiki/Poland");

        await _syncBatch.Received(2).QueueUpdateAsync(voivodeship, Arg.Any<CancellationToken>());
        await _syncBatch.Received(2).QueueUpdateAsync(country, Arg.Any<CancellationToken>());
        await _syncBatch.Received(1).FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAllAsync_SkipsItem_WhenWikipediaReturnsNoSummary()
    {
        var country = MakeCountry("Atlantis", "XX");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns((WikiPageSummary?)null);

        var result = await CreateService().SyncAllAsync();

        result.Skipped.Should().Be(2);
        result.Succeeded.Should().Be(0);
        result.Errors.Should().HaveCount(2).And.OnlyContain(e => e.Contains("Atlantis"));
        country.Description.Should().BeNull();

        await _syncBatch.DidNotReceive().QueueUpdateAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAllAsync_ContinuesRun_WhenSingleItemFails()
    {
        var failing = MakeCountry("Poland", "PL");
        var succeeding = MakeCountry("Germany", "DE");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([failing, succeeding]);
        _wikipediaService.GetPageSummaryAsync("Poland", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("429 Too Many Requests"));
        _wikipediaService.GetPageSummaryAsync("Germany", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(MakeSummary("Germany"));

        var result = await CreateService().SyncAllAsync();

        result.Failed.Should().Be(2);
        result.Succeeded.Should().Be(2);
        result.Errors.Should().HaveCount(2).And.OnlyContain(e => e.Contains("Poland"));
        succeeding.Description.Should().Be("Germany description.");
    }

    [Fact]
    public async Task SyncAllAsync_Retries_WhenWikipediaRateLimitsThenSucceeds()
    {
        var country = MakeCountry("Uzbekistan", "UZ");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync("Uzbekistan", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new HttpRequestException(
                    "Response status code does not indicate success: 429 (Too Many Requests).",
                    null,
                    System.Net.HttpStatusCode.TooManyRequests),
                _ => MakeSummary("Uzbekistan"));

        var result = await CreateService().SyncAllAsync();

        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);
        country.Description.Should().Be("Uzbekistan description.");
        country.DescriptionPl.Should().Be("Uzbekistan description.");
        await _wikipediaService.Received(3).GetPageSummaryAsync("Uzbekistan", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAllAsync_KeepsOtherWorkersBusy_WhileOneItemWaits()
    {
        var waiting = MakeCountry("Uzbekistan", "UZ");
        var fast = MakeCountry("Poland", "PL");
        var uzbekistanHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var polandFetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([waiting, fast]);
        _wikipediaService.GetPageSummaryAsync("Uzbekistan", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(_ => HoldThenSummarize("Uzbekistan"));
        _wikipediaService.GetPageSummaryAsync("Poland", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                polandFetched.TrySetResult();
                return MakeSummary("Poland");
            });

        var sync = CreateService(maxConcurrency: 2).SyncAllAsync();

        await polandFetched.Task.WaitAsync(TimeSpan.FromSeconds(5));
        uzbekistanHold.TrySetResult();

        var result = await sync;
        result.Succeeded.Should().Be(4);

        async Task<WikiPageSummary?> HoldThenSummarize(string title)
        {
            await uzbekistanHold.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return MakeSummary(title);
        }
    }

    [Fact]
    public async Task SyncAllAsync_TreatsHttpTimeoutAsItemFailure_NotAsCancellation()
    {
        // HttpClient timeouts surface as TaskCanceledException, which derives from
        // OperationCanceledException. They must not abort the whole run.
        var timingOut = MakeCountry("Poland", "PL");
        var succeeding = MakeCountry("Germany", "DE");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([timingOut, succeeding]);
        _wikipediaService.GetPageSummaryAsync("Poland", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));
        _wikipediaService.GetPageSummaryAsync("Germany", Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(MakeSummary("Germany"));

        var result = await CreateService().SyncAllAsync();

        result.Failed.Should().Be(2);
        result.Succeeded.Should().Be(2);
    }

    [Fact]
    public async Task SyncAllAsync_Aborts_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        var country = MakeCountry("Poland", "PL");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns<WikiPageSummary?>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var act = () => CreateService().SyncAllAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SyncAllAsync_WritesAuditEntries_ForStartAndCompletion()
    {
        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        await CreateService().SyncAllAsync();

        await _appLogRepository.Received(2).AddAsync(
            Arg.Is<AppLog>(l => l.Source == "DataSync"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAllAsync_Survives_AuditRepositoryFailures()
    {
        var country = MakeCountry("Poland", "PL");

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(MakeSummary("Poland"));
        _appLogRepository.AddAsync(Arg.Any<AppLog>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var result = await CreateService().SyncAllAsync();

        result.Succeeded.Should().Be(2);
    }

    [Fact]
    public async Task SyncAllAsync_ReportsProgress()
    {
        var voivodeship = MakeVoivodeship("Województwo mazowieckie", "Masovian Voivodeship");
        var country = MakeCountry("Poland", "PL");
        var progress = Substitute.For<ISyncProgress>();

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([voivodeship]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([country]);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(call => MakeSummary(call.Arg<string>()));

        var service = new DataSyncService(
            _voivodeshipRepository,
            _countryRepository,
            _wikipediaService,
            _appLogRepository,
            _syncBatch,
            NullLogger<DataSyncService>.Instance,
            progress: progress,
            maxConcurrency: 8,
            rateLimitRetryDelay: TimeSpan.Zero);

        await service.SyncAllAsync();

        progress.Received(1).Begin(4);
        progress.Received(2).ItemStarted("Województwo mazowieckie");
        progress.Received(2).ItemStarted("Poland");
        progress.Received(4).ItemFinished(true, false);
        progress.Received(1).Complete(Arg.Is<SyncResult>(r => r.Succeeded == 4));
    }

    [Fact]
    public async Task SyncAllAsync_FetchesWikipediaPagesConcurrently()
    {
        const int count = 16;
        var countries = Enumerable.Range(1, 8)
            .Select(i => MakeCountry($"Country{i}", $"C{i:00}"))
            .ToList();

        var inFlight = 0;
        var maxInFlight = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _voivodeshipRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _countryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(countries);
        _wikipediaService.GetPageSummaryAsync(Arg.Any<string>(), Arg.Any<WikiLanguage>(), Arg.Any<CancellationToken>())
            .Returns(call => FetchWhenAllStarted(call.Arg<string>()));

        var result = await CreateService(maxConcurrency: count).SyncAllAsync();

        result.Succeeded.Should().Be(count);
        maxInFlight.Should().Be(count, "all Wikipedia fetches should overlap instead of running one by one");
        await _syncBatch.Received(count).QueueUpdateAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());

        async Task<WikiPageSummary?> FetchWhenAllStarted(string title)
        {
            var now = Interlocked.Increment(ref inFlight);
            UpdateMax(ref maxInFlight, now);
            try
            {
                if (now == count)
                {
                    allStarted.TrySetResult();
                }

                await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return MakeSummary(title);
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        }
    }

    private static void UpdateMax(ref int target, int value)
    {
        int snapshot;
        do
        {
            snapshot = Volatile.Read(ref target);
            if (snapshot >= value)
            {
                return;
            }
        } while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
    }
}
