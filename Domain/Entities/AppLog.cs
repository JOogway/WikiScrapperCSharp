using WikiScrapper.Domain;

namespace WikiScrapper.Domain.Entities;

/// <summary>
/// An application-level audit entry surfaced in the Logs view.
/// Complements (does not replace) the Serilog file/console sinks.
/// </summary>
public class AppLog : Entity
{
    /// <summary>Severity of the entry.</summary>
    public required AppLogLevel Level { get; set; }

    /// <summary>Bootstrap badge CSS class for the dashboard and logs views.</summary>
    public string LevelBadgeClass => Level switch
    {
        AppLogLevel.Error => "bg-danger",
        AppLogLevel.Warning => "bg-warning text-dark",
        _ => "bg-secondary",
    };

    /// <summary>Human-readable description of what happened.</summary>
    public required string Message { get; set; }

    /// <summary>Component that produced the entry, e.g. "DataSync" or "WikipediaService".</summary>
    public required string Source { get; set; }

    /// <summary>Optional exception details when the entry represents a failure.</summary>
    public string? Exception { get; set; }
}
