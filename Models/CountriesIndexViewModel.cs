using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;

namespace WikiScrapper.Models;

/// <summary>View model for the paginated, searchable countries page.</summary>
public class CountriesIndexViewModel
{
    /// <summary>The current page of countries (empty when <see cref="VirtualizeAll"/>), already language-resolved.</summary>
    public required PagedResult<CountryDto> Countries { get; init; }

    /// <summary>The active search term, if any.</summary>
    public string? Search { get; init; }

    /// <summary>Fetched-status filter: "yes", "no", or null for all.</summary>
    public string? Fetched { get; init; }

    /// <summary>Active sort column: name, code, or fetched.</summary>
    public required string Sort { get; init; }

    /// <summary>Active sort direction: asc or desc.</summary>
    public required string Dir { get; init; }

    /// <summary>When true, the table uses client virtualization with chunked API fetches.</summary>
    public bool VirtualizeAll { get; init; }

    /// <summary>API page size used for each virtualization chunk.</summary>
    public int ChunkSize { get; init; } = 50;

    /// <summary>Value for the page-size query string (<c>all</c> or a number).</summary>
    public string PageSizeValue => VirtualizeAll ? "all" : Countries.PageSize.ToString();

    /// <summary>Route values for pagination that preserve filters and sort.</summary>
    public Dictionary<string, string> PageRoute(int page)
    {
        var values = BaseRoute();
        values["page"] = page.ToString();
        return values;
    }

    /// <summary>Route values for a column-header click (page resets to 1, direction toggles).</summary>
    public Dictionary<string, string> SortRoute(string column)
    {
        var values = BaseRoute();
        values["page"] = "1";
        values["sort"] = CountryListSort.NormalizeColumn(column);
        values["dir"] = CountryListSort.NextDirection(Sort, Dir, column);
        return values;
    }

    /// <summary>Accessible sort state for a column header.</summary>
    public string AriaSort(string column) => CountryListSort.AriaSort(Sort, Dir, column);

    /// <summary>Visual indicator for the active sort column.</summary>
    public string SortMarker(string column) => CountryListSort.SortMarker(Sort, Dir, column);

    private Dictionary<string, string> BaseRoute()
    {
        var values = new Dictionary<string, string>
        {
            ["pageSize"] = PageSizeValue,
            ["sort"] = Sort,
            ["dir"] = Dir,
        };

        if (!string.IsNullOrWhiteSpace(Search))
        {
            values["search"] = Search;
        }

        if (!string.IsNullOrWhiteSpace(Fetched))
        {
            values["fetched"] = Fetched;
        }

        return values;
    }
}
