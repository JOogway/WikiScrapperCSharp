using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Controllers.Api;

/// <summary>REST API for Polish voivodeships.</summary>
[ApiController]
[Route("api/voivodeships")]
[Produces("application/json")]
public class VoivodeshipsApiController(IVoivodeshipRepository voivodeshipRepository) : ControllerBase
{
    /// <summary>Returns all 16 Polish voivodeships with their Wikipedia descriptions.</summary>
    /// <param name="lang">Wikipedia language: <c>en</c> (default) or <c>pl</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The list of voivodeships.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VoivodeshipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VoivodeshipDto>>> GetAll(
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        var language = WikiLanguageExtensions.Parse(lang);
        return Ok(await voivodeshipRepository.GetListAsync(language, cancellationToken));
    }

    /// <summary>Returns a single voivodeship by id.</summary>
    /// <param name="id">Voivodeship id.</param>
    /// <param name="lang">Wikipedia language: <c>en</c> (default) or <c>pl</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The voivodeship.</response>
    /// <response code="404">No voivodeship with the given id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VoivodeshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoivodeshipDto>> GetById(
        int id,
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        var language = WikiLanguageExtensions.Parse(lang);
        var voivodeship = await voivodeshipRepository.GetByIdAsync(id, cancellationToken);
        return voivodeship is null ? NotFound() : Ok(VoivodeshipDto.FromEntity(voivodeship, language));
    }
}
