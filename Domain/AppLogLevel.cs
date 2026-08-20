namespace WikiScrapper.Domain;

/// <summary>Severity of an <see cref="Entities.AppLog"/> audit entry.</summary>
public enum AppLogLevel
{
    /// <summary>Normal operational message.</summary>
    Information,

    /// <summary>Non-fatal issue, e.g. a skipped Wikipedia page.</summary>
    Warning,

    /// <summary>Failure that prevented syncing an item.</summary>
    Error,
}
