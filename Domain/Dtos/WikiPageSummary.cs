namespace WikiScrapper.Domain.Dtos;

/// <summary>
/// The relevant subset of a Wikipedia REST "page summary" response.
/// </summary>
/// <param name="Title">Display title of the article.</param>
/// <param name="Extract">Plain-text summary paragraph of the article.</param>
/// <param name="PageUrl">Canonical desktop URL of the article.</param>
public record WikiPageSummary(string Title, string Extract, string? PageUrl);
