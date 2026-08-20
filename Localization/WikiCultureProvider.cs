using Microsoft.AspNetCore.Localization;
using WikiScrapper.Services;

namespace WikiScrapper.Localization;

/// <summary>Maps the <c>wiki_lang</c> cookie to the ASP.NET Core UI culture.</summary>
public sealed class WikiCultureProvider : RequestCultureProvider
{
    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(WikiLanguageAccessor.CookieName, out var lang))
        {
            var culture = string.Equals(lang, "pl", StringComparison.OrdinalIgnoreCase) ? "pl" : "en";
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
        }

        return NullProviderCultureResult;
    }
}
