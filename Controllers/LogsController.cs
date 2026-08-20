using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Controllers;

/// <summary>MVC view for recent application audit log entries.</summary>
public class LogsController(IAppLogRepository appLogRepository) : Controller
{
    /// <summary>Shows the most recent audit log entries, optionally filtered by level.</summary>
    public async Task<IActionResult> Index(string? level, CancellationToken cancellationToken)
    {
        AppLogLevel? levelFilter = Enum.TryParse<AppLogLevel>(level, ignoreCase: true, out var parsed)
            ? parsed
            : null;
        var logs = await appLogRepository.GetRecentAsync(100, levelFilter, cancellationToken);
        ViewBag.Level = levelFilter?.ToString();
        return View(logs);
    }
}
