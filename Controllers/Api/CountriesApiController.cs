using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Controllers.Api;

/// <summary>REST API for world countries.</summary>
[ApiController]
[Route("api/countries")]
[Produces("application/json")]
public class CountriesApiController(ICountryRepository countryRepository) : ControllerBase
{
    /// <summary>Returns a page of countries, optionally filtered by name or ISO code.</summary>
    /// <param name="search">Case-insensitive substring matched against name and ISO code.</param>
    /// <param name="fetched">When set, filters to countries that have (<c>true</c>) or lack (<c>false</c>) a description.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size (1-100).</param>
    /// <param name="sort">Sort column: <c>name</c> (default), <c>code</c>, or <c>fetched</c>.</param>
    /// <param name="dir">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
    /// <param name="lang">Wikipedia language for returned descriptions: <c>en</c> (default) or <c>pl</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The requested page of countries.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CountryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CountryDto>>> Get(
        [FromQuery] string? search,
        [FromQuery] bool? fetched,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        var language = WikiLanguageExtensions.Parse(lang);
        var result = await countryRepository.GetPagedAsync(
            search,
            page,
            pageSize,
            fetched,
            CountryListSort.ParseColumn(sort),
            CountryListSort.ParseDirection(dir),
            language,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single country by id.</summary>
    /// <param name="id">Country id.</param>
    /// <param name="lang">Wikipedia language: <c>en</c> (default) or <c>pl</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The country.</response>
    /// <response code="404">No country with the given id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CountryDto>> GetById(
        int id,
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        var language = WikiLanguageExtensions.Parse(lang);
        var country = await countryRepository.GetByIdAsync(id, cancellationToken);
        return country is null ? NotFound() : Ok(CountryDto.FromEntity(country, language));
    }
}
