using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Models;

namespace WikiScrapper.Controllers;

/// <summary>MVC view for world countries (searchable, sortable, paginated or virtualized).</summary>
public class CountriesController(
    ICountryRepository countryRepository,
    IWikiLanguageAccessor wikiLanguage) : Controller
{
    /// <summary>Chunk size used by the client when page size is <c>all</c>.</summary>
    public const int VirtualChunkSize = 50;

    /// <summary>Lists countries with optional search, fetched-status filter, sort, and pagination.</summary>
    public async Task<IActionResult> Index(
        string? search,
        string? fetched,
        string? sort,
        string? dir,
        int page = 1,
        string pageSize = "20",
        CancellationToken cancellationToken = default)
    {
        var virtualizeAll = string.Equals(pageSize, "all", StringComparison.OrdinalIgnoreCase);
        var resolvedPageSize = virtualizeAll
            ? VirtualChunkSize
            : pageSize is "10" or "20" or "50" ? int.Parse(pageSize) : 20;
        var fetchedFilter = fetched switch
        {
            "yes" => true,
            "no" => false,
            _ => (bool?)null,
        };
        var sortColumn = CountryListSort.ParseColumn(sort);
        var sortDir = CountryListSort.ParseDirection(dir);
        var language = wikiLanguage.Current;

        PagedResult<CountryDto> result;
        if (virtualizeAll)
        {
            var totalCount = await countryRepository.CountFilteredAsync(
                search, fetchedFilter, language, cancellationToken);
            result = new PagedResult<CountryDto>([], totalCount, 1, VirtualChunkSize);
        }
        else
        {
            result = await countryRepository.GetPagedAsync(
                search, page, resolvedPageSize, fetchedFilter, sortColumn, sortDir, language, cancellationToken);
        }

        var model = new CountriesIndexViewModel
        {
            Countries = result,
            Search = search,
            Fetched = fetchedFilter is null ? null : fetched,
            Sort = CountryListSort.ToQueryValue(sortColumn),
            Dir = CountryListSort.ToQueryValue(sortDir),
            VirtualizeAll = virtualizeAll,
            ChunkSize = VirtualChunkSize,
        };

        // Live search sends XMLHttpRequest and only needs the results fragment,
        // not the full page shell (layout, nav, scripts).
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return PartialView("_CountriesResults", model);
        }

        return View(model);
    }
}
