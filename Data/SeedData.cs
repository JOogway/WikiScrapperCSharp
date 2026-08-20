using Microsoft.EntityFrameworkCore;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Data;

/// <summary>
/// Provides the initial reference data: the 16 Polish voivodeships and the world countries
/// (193 UN member states plus Vatican City and Palestine, ISO 3166-1 alpha-2 codes).
/// Seeding is idempotent — rows are only inserted when the corresponding table is empty.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Inserts the reference data if the database is empty.
    /// The Wikipedia REST API resolves redirects, so page titles need to be close, not canonical.
    /// </summary>
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Voivodeships.AnyAsync(cancellationToken))
        {
            dbContext.Voivodeships.AddRange(GetVoivodeships());
        }

        if (!await dbContext.Countries.AnyAsync(cancellationToken))
        {
            dbContext.Countries.AddRange(GetCountries());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BackfillPolishTitlesAsync(dbContext, cancellationToken);
    }

    private static async Task BackfillPolishTitlesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var updated = false;

        foreach (var voivodeship in await dbContext.Voivodeships
                     .Where(v => v.WikiTitlePl == null || v.WikiTitlePl == string.Empty)
                     .ToListAsync(cancellationToken))
        {
            voivodeship.WikiTitlePl = voivodeship.Name;
            updated = true;
        }

        foreach (var country in await dbContext.Countries
                     .Where(c => c.WikiTitlePl == null || c.WikiTitlePl == string.Empty)
                     .ToListAsync(cancellationToken))
        {
            country.WikiTitlePl = PolishCountryWikiTitles.Get(country.Code);
            updated = true;
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>The 16 Polish voivodeships with their English Wikipedia page titles.</summary>
    private static IReadOnlyList<Voivodeship> GetVoivodeships() =>
    [
        V("Województwo dolnośląskie", "Lower Silesian Voivodeship"),
        V("Województwo kujawsko-pomorskie", "Kuyavian-Pomeranian Voivodeship"),
        V("Województwo lubelskie", "Lublin Voivodeship"),
        V("Województwo lubuskie", "Lubusz Voivodeship"),
        V("Województwo łódzkie", "Łódź Voivodeship"),
        V("Województwo małopolskie", "Lesser Poland Voivodeship"),
        V("Województwo mazowieckie", "Masovian Voivodeship"),
        V("Województwo opolskie", "Opole Voivodeship"),
        V("Województwo podkarpackie", "Podkarpackie Voivodeship"),
        V("Województwo podlaskie", "Podlaskie Voivodeship"),
        V("Województwo pomorskie", "Pomeranian Voivodeship"),
        V("Województwo śląskie", "Silesian Voivodeship"),
        V("Województwo świętokrzyskie", "Świętokrzyskie Voivodeship"),
        V("Województwo warmińsko-mazurskie", "Warmian-Masurian Voivodeship"),
        V("Województwo wielkopolskie", "Greater Poland Voivodeship"),
        V("Województwo zachodniopomorskie", "West Pomeranian Voivodeship"),
    ];

    /// <summary>World countries (UN members + Vatican City + Palestine) with ISO codes and Wikipedia titles.</summary>
    private static IReadOnlyList<Country> GetCountries() =>
    [
        C("Afghanistan", "AF"),
        C("Albania", "AL"),
        C("Algeria", "DZ"),
        C("Andorra", "AD"),
        C("Angola", "AO"),
        C("Antigua and Barbuda", "AG"),
        C("Argentina", "AR"),
        C("Armenia", "AM"),
        C("Australia", "AU"),
        C("Austria", "AT"),
        C("Azerbaijan", "AZ"),
        C("Bahamas", "BS", "The Bahamas"),
        C("Bahrain", "BH"),
        C("Bangladesh", "BD"),
        C("Barbados", "BB"),
        C("Belarus", "BY"),
        C("Belgium", "BE"),
        C("Belize", "BZ"),
        C("Benin", "BJ"),
        C("Bhutan", "BT"),
        C("Bolivia", "BO"),
        C("Bosnia and Herzegovina", "BA"),
        C("Botswana", "BW"),
        C("Brazil", "BR"),
        C("Brunei", "BN"),
        C("Bulgaria", "BG"),
        C("Burkina Faso", "BF"),
        C("Burundi", "BI"),
        C("Cabo Verde", "CV", "Cape Verde"),
        C("Cambodia", "KH"),
        C("Cameroon", "CM"),
        C("Canada", "CA"),
        C("Central African Republic", "CF"),
        C("Chad", "TD"),
        C("Chile", "CL"),
        C("China", "CN"),
        C("Colombia", "CO"),
        C("Comoros", "KM"),
        C("Congo (Republic)", "CG", "Republic of the Congo"),
        C("Congo (Democratic Republic)", "CD", "Democratic Republic of the Congo"),
        C("Costa Rica", "CR"),
        C("Côte d'Ivoire", "CI", "Ivory Coast"),
        C("Croatia", "HR"),
        C("Cuba", "CU"),
        C("Cyprus", "CY"),
        C("Czechia", "CZ", "Czech Republic"),
        C("Denmark", "DK"),
        C("Djibouti", "DJ"),
        C("Dominica", "DM"),
        C("Dominican Republic", "DO"),
        C("Ecuador", "EC"),
        C("Egypt", "EG"),
        C("El Salvador", "SV"),
        C("Equatorial Guinea", "GQ"),
        C("Eritrea", "ER"),
        C("Estonia", "EE"),
        C("Eswatini", "SZ"),
        C("Ethiopia", "ET"),
        C("Fiji", "FJ"),
        C("Finland", "FI"),
        C("France", "FR"),
        C("Gabon", "GA"),
        C("Gambia", "GM", "The Gambia"),
        C("Georgia", "GE", "Georgia (country)"),
        C("Germany", "DE"),
        C("Ghana", "GH"),
        C("Greece", "GR"),
        C("Grenada", "GD"),
        C("Guatemala", "GT"),
        C("Guinea", "GN"),
        C("Guinea-Bissau", "GW"),
        C("Guyana", "GY"),
        C("Haiti", "HT"),
        C("Honduras", "HN"),
        C("Hungary", "HU"),
        C("Iceland", "IS"),
        C("India", "IN"),
        C("Indonesia", "ID"),
        C("Iran", "IR"),
        C("Iraq", "IQ"),
        C("Ireland", "IE", "Republic of Ireland"),
        C("Israel", "IL"),
        C("Italy", "IT"),
        C("Jamaica", "JM"),
        C("Japan", "JP"),
        C("Jordan", "JO"),
        C("Kazakhstan", "KZ"),
        C("Kenya", "KE"),
        C("Kiribati", "KI"),
        C("North Korea", "KP"),
        C("South Korea", "KR"),
        C("Kuwait", "KW"),
        C("Kyrgyzstan", "KG"),
        C("Laos", "LA"),
        C("Latvia", "LV"),
        C("Lebanon", "LB"),
        C("Lesotho", "LS"),
        C("Liberia", "LR"),
        C("Libya", "LY"),
        C("Liechtenstein", "LI"),
        C("Lithuania", "LT"),
        C("Luxembourg", "LU"),
        C("Madagascar", "MG"),
        C("Malawi", "MW"),
        C("Malaysia", "MY"),
        C("Maldives", "MV"),
        C("Mali", "ML"),
        C("Malta", "MT"),
        C("Marshall Islands", "MH"),
        C("Mauritania", "MR"),
        C("Mauritius", "MU"),
        C("Mexico", "MX"),
        C("Micronesia", "FM", "Federated States of Micronesia"),
        C("Moldova", "MD"),
        C("Monaco", "MC"),
        C("Mongolia", "MN"),
        C("Montenegro", "ME"),
        C("Morocco", "MA"),
        C("Mozambique", "MZ"),
        C("Myanmar", "MM"),
        C("Namibia", "NA"),
        C("Nauru", "NR"),
        C("Nepal", "NP"),
        C("Netherlands", "NL"),
        C("New Zealand", "NZ"),
        C("Nicaragua", "NI"),
        C("Niger", "NE"),
        C("Nigeria", "NG"),
        C("North Macedonia", "MK"),
        C("Norway", "NO"),
        C("Oman", "OM"),
        C("Pakistan", "PK"),
        C("Palau", "PW"),
        C("Palestine", "PS", "State of Palestine"),
        C("Panama", "PA"),
        C("Papua New Guinea", "PG"),
        C("Paraguay", "PY"),
        C("Peru", "PE"),
        C("Philippines", "PH"),
        C("Poland", "PL"),
        C("Portugal", "PT"),
        C("Qatar", "QA"),
        C("Romania", "RO"),
        C("Russia", "RU"),
        C("Rwanda", "RW"),
        C("Saint Kitts and Nevis", "KN"),
        C("Saint Lucia", "LC"),
        C("Saint Vincent and the Grenadines", "VC"),
        C("Samoa", "WS"),
        C("San Marino", "SM"),
        C("São Tomé and Príncipe", "ST", "São Tomé and Príncipe"),
        C("Saudi Arabia", "SA"),
        C("Senegal", "SN"),
        C("Serbia", "RS"),
        C("Seychelles", "SC"),
        C("Sierra Leone", "SL"),
        C("Singapore", "SG"),
        C("Slovakia", "SK"),
        C("Slovenia", "SI"),
        C("Solomon Islands", "SB"),
        C("Somalia", "SO"),
        C("South Africa", "ZA"),
        C("South Sudan", "SS"),
        C("Spain", "ES"),
        C("Sri Lanka", "LK"),
        C("Sudan", "SD"),
        C("Suriname", "SR"),
        C("Sweden", "SE"),
        C("Switzerland", "CH"),
        C("Syria", "SY"),
        C("Tajikistan", "TJ"),
        C("Tanzania", "TZ"),
        C("Thailand", "TH"),
        C("Timor-Leste", "TL", "East Timor"),
        C("Togo", "TG"),
        C("Tonga", "TO"),
        C("Trinidad and Tobago", "TT"),
        C("Tunisia", "TN"),
        C("Turkey", "TR"),
        C("Turkmenistan", "TM"),
        C("Tuvalu", "TV"),
        C("Uganda", "UG"),
        C("Ukraine", "UA"),
        C("United Arab Emirates", "AE"),
        C("United Kingdom", "GB"),
        C("United States", "US"),
        C("Uruguay", "UY"),
        C("Uzbekistan", "UZ"),
        C("Vanuatu", "VU"),
        C("Vatican City", "VA"),
        C("Venezuela", "VE"),
        C("Vietnam", "VN"),
        C("Yemen", "YE"),
        C("Zambia", "ZM"),
        C("Zimbabwe", "ZW"),
    ];

    private static Voivodeship V(string name, string wikiTitle) =>
        new() { Name = name, WikiTitle = wikiTitle, WikiTitlePl = name };

    private static Country C(string name, string code, string? wikiTitle = null) =>
        new()
        {
            Name = name,
            Code = code,
            WikiTitle = wikiTitle ?? name,
            WikiTitlePl = PolishCountryWikiTitles.Get(code),
        };
}
