using WikiScrapper.Domain;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Services;

/// <summary>Cookie-backed Wikipedia language preference for the current HTTP request.</summary>
public sealed class WikiLanguageAccessor(IHttpContextAccessor httpContextAccessor) : IWikiLanguageAccessor
{
    /// <summary>Cookie name storing the selected language code.</summary>
    public const string CookieName = "wiki_lang";

    /// <inheritdoc />
    public WikiLanguage Current
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                return WikiLanguage.En;
            }

            if (request.Query.TryGetValue("lang", out var queryLang))
            {
                return WikiLanguageExtensions.Parse(queryLang);
            }

            if (request.Cookies.TryGetValue(CookieName, out var cookieLang))
            {
                return WikiLanguageExtensions.Parse(cookieLang);
            }

            return WikiLanguage.En;
        }
    }
}
