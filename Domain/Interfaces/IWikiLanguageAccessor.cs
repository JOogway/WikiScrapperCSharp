using WikiScrapper.Domain;

namespace WikiScrapper.Domain.Interfaces;

/// <summary>Reads the user's selected Wikipedia display language from the current request.</summary>
public interface IWikiLanguageAccessor
{
    /// <summary>Active language for UI display (defaults to English).</summary>
    WikiLanguage Current { get; }
}
