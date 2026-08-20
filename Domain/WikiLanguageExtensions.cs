namespace WikiScrapper.Domain;

/// <summary>Helpers for parsing and displaying <see cref="WikiLanguage"/> values.</summary>
public static class WikiLanguageExtensions
{
    /// <summary>Cookie/query value for English.</summary>
    public const string EnCode = "en";

    /// <summary>Cookie/query value for Polish.</summary>
    public const string PlCode = "pl";

    /// <summary>Parses a cookie or query value; unknown values fall back to English.</summary>
    public static WikiLanguage Parse(string? value) =>
        string.Equals(value, PlCode, StringComparison.OrdinalIgnoreCase) ? WikiLanguage.Pl : WikiLanguage.En;

    /// <summary>Stable lowercase code for cookies and URLs.</summary>
    public static string ToCode(this WikiLanguage language) =>
        language == WikiLanguage.Pl ? PlCode : EnCode;

    /// <summary>Human-readable label for the UI dropdown.</summary>
    public static string ToDisplayName(this WikiLanguage language) =>
        language == WikiLanguage.Pl ? "Polski" : "English";
}
