using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WikiScrapper.Domain.Interfaces;
using WikiScrapper.Models;

namespace WikiScrapper.Controllers;

/// <summary>Dashboard: sync trigger and overall status.</summary>
public class HomeController(
    IVoivodeshipRepository voivodeshipRepository,
    ICountryRepository countryRepository,
    IAppLogRepository appLogRepository,
    ISyncJobService syncJobService,
    IWikiLanguageAccessor wikiLanguage,
    IStringLocalizer<SharedResources> localizer,
    ILogger<HomeController> logger) : Controller
{
    /// <summary>Shows the dashboard with sync status and recent log entries.</summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var language = wikiLanguage.Current;
        var voivodeshipStats = await voivodeshipRepository.GetFetchStatsAsync(language, cancellationToken);
        var countryStats = await countryRepository.GetFetchStatsAsync(language, cancellationToken);
        var recentLogs = await appLogRepository.GetRecentAsync(10, level: null, cancellationToken);

        var lastFetchedAt = new[] { voivodeshipStats.LastFetchedAt, countryStats.LastFetchedAt }
            .Where(d => d.HasValue)
            .Max();

        var model = new DashboardViewModel
        {
            VoivodeshipCount = voivodeshipStats.Total,
            VoivodeshipsFetched = voivodeshipStats.Fetched,
            CountryCount = countryStats.Total,
            CountriesFetched = countryStats.Fetched,
            LastFetchedAt = lastFetchedAt,
            RecentLogs = recentLogs,
            SyncStatus = syncJobService.GetStatus(),
        };

        return View(model);
    }

    /// <summary>
    /// Starts a background synchronization and redirects to the dashboard,
    /// which polls for progress. Used as a no-JavaScript fallback.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Sync()
    {
        if (!syncJobService.TryStart())
        {
            TempData["SyncMessage"] = localizer["Dashboard.SyncAlreadyRunning"].Value;
            TempData["SyncSuccess"] = false;
        }
        else
        {
            logger.LogInformation("Synchronization queued from the dashboard");
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Standard error page.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
