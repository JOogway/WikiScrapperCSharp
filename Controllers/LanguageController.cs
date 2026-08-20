using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain;
using WikiScrapper.Services;

namespace WikiScrapper.Controllers;

/// <summary>Sets the Wikipedia display language cookie and redirects back.</summary>
public class LanguageController : Controller
{
    /// <summary>Persists the language preference and returns to the previous page.</summary>
    [HttpGet("/language")]
    public IActionResult Set([FromQuery] string lang, [FromQuery] string? returnUrl = "/")
    {
        if (!IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        Response.Cookies.Append(
            WikiLanguageAccessor.CookieName,
            WikiLanguageExtensions.Parse(lang).ToCode(),
            new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
            });

        return Redirect(returnUrl);
    }

    private bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) &&
        (Url.IsLocalUrl(url) || (url.StartsWith('/') && !url.StartsWith("//")));
}
