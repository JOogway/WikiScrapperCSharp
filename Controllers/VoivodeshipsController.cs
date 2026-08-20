using Microsoft.AspNetCore.Mvc;
using WikiScrapper.Domain.Interfaces;

namespace WikiScrapper.Controllers;

/// <summary>MVC view for the 16 Polish voivodeships.</summary>
public class VoivodeshipsController(
    IVoivodeshipRepository voivodeshipRepository,
    IWikiLanguageAccessor wikiLanguage) : Controller
{
    /// <summary>Lists all voivodeships with their fetched descriptions.</summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var voivodeships = await voivodeshipRepository.GetListAsync(wikiLanguage.Current, cancellationToken);
        return View(voivodeships);
    }
}
