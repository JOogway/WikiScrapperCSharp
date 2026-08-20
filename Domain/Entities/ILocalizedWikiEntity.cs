using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Entities;

/// <summary>Entity with English and Polish Wikipedia summaries.</summary>
public interface ILocalizedWikiEntity
{
    /// <summary>Returns the stored description for the given language.</summary>
    string? GetDescription(WikiLanguage language);

    /// <summary>Returns the Wikipedia article URL for the given language.</summary>
    string? GetWikiUrl(WikiLanguage language);

    /// <summary>Returns the last fetch timestamp for the given language.</summary>
    DateTime? GetFetchedAt(WikiLanguage language);

    /// <summary>Returns the Wikipedia page title used when fetching the given language.</summary>
    string GetWikiTitle(WikiLanguage language);

    /// <summary>True when a description has been fetched for the given language.</summary>
    bool IsFetched(WikiLanguage language);

    /// <summary>Applies a fetched summary to the fields for the given language.</summary>
    void ApplySummary(WikiPageSummary summary, WikiLanguage language);
}
