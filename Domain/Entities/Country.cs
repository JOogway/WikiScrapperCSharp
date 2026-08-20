using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Domain.Entities;

/// <summary>
/// A world country together with its English and Polish Wikipedia descriptions.
/// </summary>
public class Country : Entity, ILocalizedWikiEntity
{
    /// <summary>English short name of the country, e.g. "Poland".</summary>
    public required string Name { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, e.g. "PL".</summary>
    public required string Code { get; set; }

    /// <summary>English Wikipedia page title, e.g. "Poland".</summary>
    public required string WikiTitle { get; set; }

    /// <summary>Polish Wikipedia page title, e.g. "Polska".</summary>
    public required string WikiTitlePl { get; set; }

    /// <summary>English article URL after a successful fetch.</summary>
    public string? WikiUrl { get; set; }

    /// <summary>English summary from Wikipedia.</summary>
    public string? Description { get; set; }

    /// <summary>UTC timestamp of the last successful English fetch.</summary>
    public DateTime? FetchedAt { get; set; }

    /// <summary>Polish article URL after a successful fetch.</summary>
    public string? WikiUrlPl { get; set; }

    /// <summary>Polish summary from Wikipedia.</summary>
    public string? DescriptionPl { get; set; }

    /// <summary>UTC timestamp of the last successful Polish fetch.</summary>
    public DateTime? FetchedAtPl { get; set; }

    /// <inheritdoc />
    public string? GetDescription(WikiLanguage language) =>
        language == WikiLanguage.Pl ? DescriptionPl : Description;

    /// <inheritdoc />
    public string? GetWikiUrl(WikiLanguage language) =>
        language == WikiLanguage.Pl ? WikiUrlPl : WikiUrl;

    /// <inheritdoc />
    public DateTime? GetFetchedAt(WikiLanguage language) =>
        language == WikiLanguage.Pl ? FetchedAtPl : FetchedAt;

    /// <inheritdoc />
    public string GetWikiTitle(WikiLanguage language) =>
        language == WikiLanguage.Pl ? WikiTitlePl : WikiTitle;

    /// <inheritdoc />
    public bool IsFetched(WikiLanguage language) => GetDescription(language) is not null;

    /// <inheritdoc />
    public void ApplySummary(WikiPageSummary summary, WikiLanguage language)
    {
        if (language == WikiLanguage.Pl)
        {
            DescriptionPl = summary.Extract;
            WikiUrlPl = summary.PageUrl;
            FetchedAtPl = DateTime.UtcNow;
            return;
        }

        Description = summary.Extract;
        WikiUrl = summary.PageUrl;
        FetchedAt = DateTime.UtcNow;
    }
}
