using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WikiScrapper.Services;
using WikiScrapper.Tests.TestHelpers;

namespace WikiScrapper.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WikipediaService"/> using a stubbed <see cref="HttpMessageHandler"/>.
/// </summary>
public class WikipediaServiceTests
{
    private const string PolandSummaryJson = """
        {
            "title": "Poland",
            "extract": "Poland is a country in Central Europe.",
            "content_urls": {
                "desktop": { "page": "https://en.wikipedia.org/wiki/Poland" }
            }
        }
        """;

    private static WikipediaService CreateService(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://en.wikipedia.org/api/rest_v1/"),
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(WikipediaClientOptions.EnClientName).Returns(httpClient);
        factory.CreateClient(WikipediaClientOptions.PlClientName).Returns(httpClient);
        return new WikipediaService(factory, NullLogger<WikipediaService>.Instance);
    }

    [Fact]
    public async Task GetPageSummaryAsync_ReturnsSummary_WhenPageExists()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, PolandSummaryJson);
        var service = CreateService(handler);

        var summary = await service.GetPageSummaryAsync("Poland");

        summary.Should().NotBeNull();
        summary.Title.Should().Be("Poland");
        summary.Extract.Should().Be("Poland is a country in Central Europe.");
        summary.PageUrl.Should().Be("https://en.wikipedia.org/wiki/Poland");
    }

    [Fact]
    public async Task GetPageSummaryAsync_EncodesSpacesAsUnderscores()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, PolandSummaryJson);
        var service = CreateService(handler);

        await service.GetPageSummaryAsync("Masovian Voivodeship");

        handler.Requests.Should().ContainSingle()
            .Which.RequestUri!.AbsolutePath.Should().Contain("Masovian_Voivodeship");
    }

    [Fact]
    public async Task GetPageSummaryAsync_ReturnsNull_WhenPageNotFound()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.NotFound);
        var service = CreateService(handler);

        var summary = await service.GetPageSummaryAsync("Nonexistent Page XYZ");

        summary.Should().BeNull();
    }

    [Fact]
    public async Task GetPageSummaryAsync_ReturnsNull_WhenExtractIsEmpty()
    {
        var handler = StubHttpMessageHandler.Returning(
            HttpStatusCode.OK, """{ "title": "Empty", "extract": "" }""");
        var service = CreateService(handler);

        var summary = await service.GetPageSummaryAsync("Empty");

        summary.Should().BeNull();
    }

    [Fact]
    public async Task GetPageSummaryAsync_Throws_OnRateLimiting()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.TooManyRequests);
        var service = CreateService(handler);

        var act = () => service.GetPageSummaryAsync("Poland");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPageSummaryAsync_Throws_OnServerError()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var act = () => service.GetPageSummaryAsync("Poland");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPageSummaryAsync_Throws_OnBlankTitle(string title)
    {
        var service = CreateService(StubHttpMessageHandler.Returning(HttpStatusCode.OK, PolandSummaryJson));

        var act = () => service.GetPageSummaryAsync(title);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
