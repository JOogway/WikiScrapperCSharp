namespace WikiScrapper.Domain;

/// <summary>
/// Normalizes country list sort query values (code, name, fetched).
/// </summary>
public static class CountryListSort
{
    /// <summary>Sort by ISO code.</summary>
    public const string Code = "code";

    /// <summary>Sort by country name (default).</summary>
    public const string Name = "name";

    /// <summary>Sort by last successful Wikipedia fetch time.</summary>
    public const string Fetched = "fetched";

    /// <summary>Maps an incoming query value to a known column; unknown values fall back to name.</summary>
    public static CountrySortColumn ParseColumn(string? sort) =>
        NormalizeColumn(sort) switch
        {
            Code => CountrySortColumn.Code,
            Fetched => CountrySortColumn.Fetched,
            _ => CountrySortColumn.Name,
        };

    /// <summary>Maps an incoming query value to a known column string; unknown values fall back to name.</summary>
    public static string NormalizeColumn(string? sort) =>
        sort?.Trim().ToLowerInvariant() switch
        {
            Code => Code,
            Fetched => Fetched,
            _ => Name,
        };

    /// <summary>Maps a sort column enum to its query-string token.</summary>
    public static string ToQueryValue(CountrySortColumn column) =>
        column switch
        {
            CountrySortColumn.Code => Code,
            CountrySortColumn.Fetched => Fetched,
            _ => Name,
        };

    /// <summary>True when the query asks for descending order.</summary>
    public static bool IsDescending(string? dir) =>
        string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses a direction query value.</summary>
    public static SortDirection ParseDirection(string? dir) =>
        IsDescending(dir) ? SortDirection.Desc : SortDirection.Asc;

    /// <summary>Maps a direction enum to its query-string token.</summary>
    public static string ToQueryValue(SortDirection direction) =>
        direction == SortDirection.Desc ? "desc" : "asc";

    /// <summary>
    /// Next direction after clicking a column header: a new column starts ascending
    /// (fetched starts descending, newest first); the active column toggles.
    /// </summary>
    public static string NextDirection(string? currentColumn, string? currentDir, string clickedColumn)
    {
        var current = NormalizeColumn(currentColumn);
        var clicked = NormalizeColumn(clickedColumn);
        if (current != clicked)
        {
            return clicked == Fetched ? "desc" : "asc";
        }

        return IsDescending(currentDir) ? "asc" : "desc";
    }

    /// <summary>ARIA sort attribute for a column header.</summary>
    public static string AriaSort(string? sort, string? dir, string column)
    {
        if (NormalizeColumn(sort) != NormalizeColumn(column))
        {
            return "none";
        }

        return IsDescending(dir) ? "descending" : "ascending";
    }

    /// <summary>Visual marker for the active sort column.</summary>
    public static string SortMarker(string? sort, string? dir, string column) =>
        AriaSort(sort, dir, column) switch
        {
            "ascending" => " ↑",
            "descending" => " ↓",
            _ => "",
        };
}
