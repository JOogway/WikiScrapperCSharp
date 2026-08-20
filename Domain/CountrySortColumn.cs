namespace WikiScrapper.Domain;

/// <summary>Sortable columns for the countries list.</summary>
public enum CountrySortColumn
{
    /// <summary>Sort by country name.</summary>
    Name,

    /// <summary>Sort by ISO 3166-1 alpha-2 code.</summary>
    Code,

    /// <summary>Sort by last successful Wikipedia fetch time.</summary>
    Fetched,
}
