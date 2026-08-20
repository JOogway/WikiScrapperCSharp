using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>
/// Client for fetching page summaries from the Wikipedia REST API.
/// </summary>
public interface IWikipediaService
{
    /// <summary>
    /// Fetches the summary of a Wikipedia article by page title and language.
    /// </summary>
    Task<WikiPageSummary?> GetPageSummaryAsync(
        string pageTitle,
        WikiLanguage language = WikiLanguage.En,
        CancellationToken cancellationToken = default);
}
