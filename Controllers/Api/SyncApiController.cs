using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Controllers.Api;

/// <summary>REST API for triggering and observing Wikipedia data synchronization.</summary>
[ApiController]
[Route("api/sync")]
[Produces("application/json")]
public class SyncApiController(ISyncJobService syncJobService) : ControllerBase
{
    /// <summary>
    /// Starts a background synchronization of all voivodeships and countries.
    /// The request returns immediately; poll <c>GET /api/sync/status</c> for progress.
    /// Individual item failures are collected and do not abort the run.
    /// </summary>
    /// <response code="202">The job was accepted and is running.</response>
    /// <response code="409">A synchronization is already in progress.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SyncStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(SyncStatusDto), StatusCodes.Status409Conflict)]
    public ActionResult<SyncStatusDto> Start()
    {
        if (!syncJobService.TryStart())
        {
            return Conflict(syncJobService.GetStatus());
        }

        return AcceptedAtAction(nameof(GetStatus), syncJobService.GetStatus());
    }

    /// <summary>Returns live progress of the current run, or the result of the last completed run.</summary>
    /// <response code="200">Current synchronization status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SyncStatusDto), StatusCodes.Status200OK)]
    public ActionResult<SyncStatusDto> GetStatus() => Ok(syncJobService.GetStatus());
}
