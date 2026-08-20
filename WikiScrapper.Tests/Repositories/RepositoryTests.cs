using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;
using WikiScrapper.Data;
using WikiScrapper.Data.Repositories;

namespace WikiScrapper.Tests.Repositories;

/// <summary>
/// Repository tests against the EF Core in-memory provider.
/// </summary>
public class RepositoryTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Country MakeCountry(string name, string code) =>
        new() { Name = name, Code = code, WikiTitle = name, WikiTitlePl = name };

    [Fact]
    public async Task VoivodeshipRepository_GetAllAsync_ReturnsAllOrderedByName()
    {
        await using var context = CreateContext();
        context.Voivodeships.AddRange(
            new Voivodeship { Name = "Województwo śląskie", WikiTitle = "Silesian Voivodeship", WikiTitlePl = "Województwo śląskie" },
            new Voivodeship { Name = "Województwo lubuskie", WikiTitle = "Lubusz Voivodeship", WikiTitlePl = "Województwo lubuskie" });
        await context.SaveChangesAsync();

        var repository = new VoivodeshipRepository(context);
        var result = await repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Województwo lubuskie");
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_FiltersBySearchTerm()
    {
        await using var context = CreateContext();
        context.Countries.AddRange(
            MakeCountry("Poland", "PL"),
            MakeCountry("Portugal", "PT"),
            MakeCountry("Germany", "DE"));
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);
        var result = await repository.GetPagedAsync("Po", page: 1, pageSize: 10);

        result.TotalCount.Should().Be(2);
        result.Items.Select(c => c.Name).Should().BeEquivalentTo("Poland", "Portugal");
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_PaginatesAndReportsMetadata()
    {
        await using var context = CreateContext();
        context.Countries.AddRange(Enumerable.Range(1, 25)
            .Select(i => MakeCountry($"Country {i:D2}", $"{(char)('A' + i / 26)}{(char)('A' + i % 26)}")));
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);
        var page2 = await repository.GetPagedAsync(search: null, page: 2, pageSize: 10);

        page2.Items.Should().HaveCount(10);
        page2.TotalCount.Should().Be(25);
        page2.TotalPages.Should().Be(3);
        page2.HasPrevious.Should().BeTrue();
        page2.HasNext.Should().BeTrue();
        page2.Items[0].Name.Should().Be("Country 11");
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_ClampsInvalidPaging()
    {
        await using var context = CreateContext();
        context.Countries.Add(MakeCountry("Poland", "PL"));
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);
        var result = await repository.GetPagedAsync(search: null, page: -5, pageSize: 0);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_FiltersByFetchedStatus()
    {
        await using var context = CreateContext();
        var fetched = MakeCountry("Poland", "PL");
        fetched.Description = "A country in Central Europe.";
        context.Countries.AddRange(fetched, MakeCountry("Germany", "DE"));
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);

        var missing = await repository.GetPagedAsync(search: null, page: 1, pageSize: 10, fetched: false);
        missing.TotalCount.Should().Be(1);
        missing.Items.Single().Name.Should().Be("Germany");

        var present = await repository.GetPagedAsync(search: null, page: 1, pageSize: 10, fetched: true);
        present.Items.Single().Name.Should().Be("Poland");
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_SortsByCodeDescending()
    {
        await using var context = CreateContext();
        context.Countries.AddRange(
            MakeCountry("Poland", "PL"),
            MakeCountry("Germany", "DE"),
            MakeCountry("Austria", "AT"));
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);
        var result = await repository.GetPagedAsync(
            search: null, page: 1, pageSize: 10, sort: CountrySortColumn.Code, dir: SortDirection.Desc);

        result.Items.Select(c => c.Code).Should().Equal("PL", "DE", "AT");
    }

    [Fact]
    public async Task CountryRepository_GetPagedAsync_SortsByFetchedNewestFirst()
    {
        await using var context = CreateContext();
        var older = MakeCountry("Germany", "DE");
        older.FetchedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = MakeCountry("Poland", "PL");
        newer.FetchedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var missing = MakeCountry("Austria", "AT");
        context.Countries.AddRange(older, newer, missing);
        await context.SaveChangesAsync();

        var repository = new CountryRepository(context);
        var result = await repository.GetPagedAsync(
            search: null, page: 1, pageSize: 10, sort: CountrySortColumn.Fetched, dir: SortDirection.Desc);

        result.Items.Select(c => c.Name).Should().Equal("Poland", "Germany", "Austria");
    }

    [Fact]
    public async Task AppLogRepository_GetRecentAsync_ReturnsNewestFirst()
    {
        await using var context = CreateContext();
        var repository = new AppLogRepository(context);

        await repository.AddAsync(new AppLog { Level = AppLogLevel.Information, Message = "first", Source = "Test" });
        await repository.AddAsync(new AppLog { Level = AppLogLevel.Error, Message = "second", Source = "Test" });

        var recent = await repository.GetRecentAsync(10);

        recent.Should().HaveCount(2);
        recent[0].Message.Should().Be("second");
    }

    [Fact]
    public async Task AppLogRepository_GetRecentAsync_FiltersByLevel()
    {
        await using var context = CreateContext();
        var repository = new AppLogRepository(context);

        await repository.AddAsync(new AppLog { Level = AppLogLevel.Information, Message = "info", Source = "Test" });
        await repository.AddAsync(new AppLog { Level = AppLogLevel.Error, Message = "boom", Source = "Test" });

        var errors = await repository.GetRecentAsync(10, AppLogLevel.Error);

        errors.Should().ContainSingle().Which.Message.Should().Be("boom");
    }

    [Fact]
    public async Task SyncDbBatch_FlushesAutomaticallyAtBatchSize_AndOnExplicitFlush()
    {
        var dbName = Guid.NewGuid().ToString();
        AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

        await using var context = NewContext();
        context.Countries.AddRange(Enumerable.Range(1, SyncDbBatch.BatchSize + 1)
            .Select(i => MakeCountry($"Country {i:D2}", $"{(char)('A' + i / 26)}{(char)('A' + i % 26)}")));
        await context.SaveChangesAsync();

        var batch = new SyncDbBatch(context);
        var countries = context.Countries.OrderBy(c => c.Name).ToList();

        foreach (var country in countries.Take(SyncDbBatch.BatchSize - 1))
        {
            country.Description = "synced";
            await batch.QueueUpdateAsync(country);
        }

        await using (var check = NewContext())
        {
            (await check.Countries.CountAsync(c => c.Description != null))
                .Should().Be(0, "nothing should be committed before the batch is full");
        }

        countries[SyncDbBatch.BatchSize - 1].Description = "synced";
        await batch.QueueUpdateAsync(countries[SyncDbBatch.BatchSize - 1]);

        await using (var check = NewContext())
        {
            (await check.Countries.CountAsync(c => c.Description != null))
                .Should().Be(SyncDbBatch.BatchSize, "reaching the batch size should trigger an automatic flush");
        }

        countries[^1].Description = "synced";
        await batch.QueueUpdateAsync(countries[^1]);
        await batch.FlushAsync();

        await using (var check = NewContext())
        {
            (await check.Countries.CountAsync(c => c.Description != null))
                .Should().Be(SyncDbBatch.BatchSize + 1, "the explicit flush should persist the partial batch");
        }
    }

    [Fact]
    public async Task SaveChanges_SetsAuditTimestamps()
    {
        await using var context = CreateContext();
        var country = MakeCountry("Poland", "PL");
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        country.CreatedAt.Should().NotBe(default);
        country.UpdatedAt.Should().Be(country.CreatedAt);

        var createdAt = country.CreatedAt;
        country.Description = "Updated description";
        await context.SaveChangesAsync();

        country.CreatedAt.Should().Be(createdAt);
        country.UpdatedAt.Should().BeAfter(createdAt);
    }

    [Fact]
    public async Task SeedData_ProvidesSixteenVoivodeshipsAndUniqueCountries()
    {
        await using var context = CreateContext();
        await SeedData.SeedAsync(context);

        (await context.Voivodeships.CountAsync()).Should().Be(16);
        var countries = await context.Countries.ToListAsync();
        countries.Should().HaveCountGreaterThanOrEqualTo(193);
        countries.Select(c => c.Code).Should().OnlyHaveUniqueItems();
        countries.Select(c => c.Name).Should().OnlyHaveUniqueItems();
    }
}
