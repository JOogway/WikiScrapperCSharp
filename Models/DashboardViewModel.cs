using WikiScrapper.Domain.Dtos;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Models;

/// <summary>View model for the dashboard page.</summary>
public class DashboardViewModel
{
    /// <summary>Total number of voivodeships in the database.</summary>
    public int VoivodeshipCount { get; init; }

    /// <summary>Number of voivodeships with a fetched description.</summary>
    public int VoivodeshipsFetched { get; init; }

    /// <summary>Total number of countries in the database.</summary>
    public int CountryCount { get; init; }

    /// <summary>Number of countries with a fetched description.</summary>
    public int CountriesFetched { get; init; }

    /// <summary>Timestamp of the most recent successful fetch across all items, if any.</summary>
    public DateTime? LastFetchedAt { get; init; }

    /// <summary>Most recent audit log entries.</summary>
    public IReadOnlyList<AppLog> RecentLogs { get; init; } = [];

    /// <summary>Live or last-completed synchronization status.</summary>
    public required SyncStatusDto SyncStatus { get; init; }

    /// <summary>True when no descriptions have been fetched yet.</summary>
    public bool IsFirstRun => VoivodeshipsFetched == 0 && CountriesFetched == 0;
}
