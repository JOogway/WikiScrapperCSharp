using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Domain.Dtos;

/// <summary>API representation of a <see cref="Voivodeship"/>.</summary>
public record VoivodeshipDto(
    int Id,
    string Name,
    string WikiTitle,
    string? WikiUrl,
    string? Description,
    DateTime? FetchedAt)
{
    /// <summary>Maps an entity to its API representation for the given language.</summary>
    public static VoivodeshipDto FromEntity(Voivodeship entity, WikiLanguage language) =>
        new(
            entity.Id,
            entity.Name,
            entity.GetWikiTitle(language),
            entity.GetWikiUrl(language),
            entity.GetDescription(language),
            entity.GetFetchedAt(language));
}

/// <summary>API representation of a <see cref="Country"/>.</summary>
public record CountryDto(
    int Id,
    string Name,
    string Code,
    string WikiTitle,
    string? WikiUrl,
    string? Description,
    DateTime? FetchedAt)
{
    /// <summary>Maps an entity to its API representation for the given language.</summary>
    public static CountryDto FromEntity(Country entity, WikiLanguage language) =>
        new(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.GetWikiTitle(language),
            entity.GetWikiUrl(language),
            entity.GetDescription(language),
            entity.GetFetchedAt(language));
}
