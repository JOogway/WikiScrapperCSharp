using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Services;

/// <summary>
/// Fetches page summaries from the English and Polish Wikipedia REST APIs.
/// </summary>
public class WikipediaService(
    IHttpClientFactory httpClientFactory,
    ILogger<WikipediaService> logger) : IWikipediaService
{
    /// <inheritdoc />
    public async Task<WikiPageSummary?> GetPageSummaryAsync(
        string pageTitle,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageTitle);

        var clientName = language == WikiLanguage.Pl
            ? WikipediaClientOptions.PlClientName
            : WikipediaClientOptions.EnClientName;
        var httpClient = httpClientFactory.CreateClient(clientName);

        var encodedTitle = Uri.EscapeDataString(pageTitle.Replace(' ', '_'));

        using var response = await httpClient.GetAsync(
            $"page/summary/{encodedTitle}?redirect=true",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Wikipedia page not found ({Language}): {PageTitle}", language, pageTitle);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<WikipediaSummaryResponse>(cancellationToken);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Extract))
        {
            logger.LogWarning("Wikipedia returned an empty summary ({Language}): {PageTitle}", language, pageTitle);
            return null;
        }

        return new WikiPageSummary(
            payload.Title ?? pageTitle,
            payload.Extract.Trim(),
            payload.Urls?.Desktop?.Page);
    }
}
