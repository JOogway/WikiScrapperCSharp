namespace WikiScrapper.Domain;

/// <summary>Polish Wikipedia article titles for world countries (ISO 3166-1 alpha-2).</summary>
public static class PolishCountryWikiTitles
{
    private static readonly IReadOnlyDictionary<string, string> ByIsoCode =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AF"] = "Afganistan", ["AL"] = "Albania", ["DZ"] = "Algieria", ["AD"] = "Andora", ["AO"] = "Angola",
            ["AG"] = "Antigua i Barbuda", ["AR"] = "Argentyna", ["AM"] = "Armenia", ["AU"] = "Australia", ["AT"] = "Austria",
            ["AZ"] = "Azerbejdżan", ["BS"] = "Bahamy", ["BH"] = "Bahrajn", ["BD"] = "Bangladesz", ["BB"] = "Barbados",
            ["BY"] = "Białoruś", ["BE"] = "Belgia", ["BZ"] = "Belize", ["BJ"] = "Benin", ["BT"] = "Bhutan",
            ["BO"] = "Boliwia", ["BA"] = "Bośnia i Hercegowina", ["BW"] = "Botswana", ["BR"] = "Brazylia", ["BN"] = "Brunei",
            ["BG"] = "Bułgaria", ["BF"] = "Burkina Faso", ["BI"] = "Burundi", ["CV"] = "Republika Zielonego Przylądka",
            ["KH"] = "Kambodża", ["CM"] = "Kamerun", ["CA"] = "Kanada", ["CF"] = "Republika Środkowoafrykańska", ["TD"] = "Czad",
            ["CL"] = "Chile", ["CN"] = "Chiny", ["CO"] = "Kolumbia", ["KM"] = "Komory", ["CG"] = "Kongo",
            ["CD"] = "Demokratyczna Republika Konga", ["CR"] = "Kostaryka", ["CI"] = "Wybrzeże Kości Słoniowej", ["HR"] = "Chorwacja",
            ["CU"] = "Kuba", ["CY"] = "Cypr", ["CZ"] = "Czechy", ["DK"] = "Dania", ["DJ"] = "Dżibuti",
            ["DM"] = "Dominika", ["DO"] = "Dominikana", ["EC"] = "Ekwador", ["EG"] = "Egipt", ["SV"] = "Salwador",
            ["GQ"] = "Gwinea Równikowa", ["ER"] = "Erytrea", ["EE"] = "Estonia", ["SZ"] = "Eswatini", ["ET"] = "Etiopia",
            ["FJ"] = "Fidżi", ["FI"] = "Finlandia", ["FR"] = "Francja", ["GA"] = "Gabon", ["GM"] = "Gambia",
            ["GE"] = "Gruzja", ["DE"] = "Niemcy", ["GH"] = "Ghana", ["GR"] = "Grecja", ["GD"] = "Grenada",
            ["GT"] = "Gwatemala", ["GN"] = "Gwinea", ["GW"] = "Gwinea-Bissau", ["GY"] = "Gujana", ["HT"] = "Haiti",
            ["HN"] = "Honduras", ["HU"] = "Węgry", ["IS"] = "Islandia", ["IN"] = "Indie", ["ID"] = "Indonezja",
            ["IR"] = "Iran", ["IQ"] = "Irak", ["IE"] = "Irlandia", ["IL"] = "Izrael", ["IT"] = "Włochy",
            ["JM"] = "Jamajka", ["JP"] = "Japonia", ["JO"] = "Jordania", ["KZ"] = "Kazachstan", ["KE"] = "Kenia",
            ["KI"] = "Kiribati", ["KP"] = "Korea Północna", ["KR"] = "Korea Południowa", ["KW"] = "Kuwejt", ["KG"] = "Kirgistan",
            ["LA"] = "Laos", ["LV"] = "Łotwa", ["LB"] = "Liban", ["LS"] = "Lesotho", ["LR"] = "Liberia",
            ["LY"] = "Libia", ["LI"] = "Liechtenstein", ["LT"] = "Litwa", ["LU"] = "Luksemburg", ["MG"] = "Madagaskar",
            ["MW"] = "Malawi", ["MY"] = "Malezja", ["MV"] = "Malediwy", ["ML"] = "Mali", ["MT"] = "Malta",
            ["MH"] = "Wyspy Marshalla", ["MR"] = "Mauretania", ["MU"] = "Mauritius", ["MX"] = "Meksyk",
            ["FM"] = "Mikronezja", ["MD"] = "Mołdawia", ["MC"] = "Monako", ["MN"] = "Mongolia", ["ME"] = "Czarnogóra",
            ["MA"] = "Maroko", ["MZ"] = "Mozambik", ["MM"] = "Mjanma", ["NA"] = "Namibia", ["NR"] = "Nauru",
            ["NP"] = "Nepal", ["NL"] = "Holandia", ["NZ"] = "Nowa Zelandia", ["NI"] = "Nikaragua", ["NE"] = "Niger",
            ["NG"] = "Nigeria", ["MK"] = "Macedonia Północna", ["NO"] = "Norwegia", ["OM"] = "Oman", ["PK"] = "Pakistan",
            ["PW"] = "Palau", ["PS"] = "Palestyna", ["PA"] = "Panama", ["PG"] = "Papua-Nowa Gwinea", ["PY"] = "Paragwaj",
            ["PE"] = "Peru", ["PH"] = "Filipiny", ["PL"] = "Polska", ["PT"] = "Portugalia", ["QA"] = "Katar",
            ["RO"] = "Rumunia", ["RU"] = "Rosja", ["RW"] = "Rwanda", ["KN"] = "Saint Kitts i Nevis", ["LC"] = "Saint Lucia",
            ["VC"] = "Saint Vincent i Grenadyny", ["WS"] = "Samoa", ["SM"] = "San Marino",
            ["ST"] = "Wyspy Świętego Tomasza i Książęca", ["SA"] = "Arabia Saudyjska", ["SN"] = "Senegal", ["RS"] = "Serbia",
            ["SC"] = "Seszele", ["SL"] = "Sierra Leone", ["SG"] = "Singapur", ["SK"] = "Słowacja", ["SI"] = "Słowenia",
            ["SB"] = "Wyspy Salomona", ["SO"] = "Somalia", ["ZA"] = "Republika Południowej Afryki", ["SS"] = "Sudan Południowy",
            ["ES"] = "Hiszpania", ["LK"] = "Sri Lanka", ["SD"] = "Sudan", ["SR"] = "Surinam", ["SE"] = "Szwecja",
            ["CH"] = "Szwajcaria", ["SY"] = "Syria", ["TJ"] = "Tadżykistan", ["TZ"] = "Tanzania", ["TH"] = "Tajlandia",
            ["TL"] = "Timor Wschodni", ["TG"] = "Togo", ["TO"] = "Tonga", ["TT"] = "Trynidad i Tobago", ["TN"] = "Tunezja",
            ["TR"] = "Turcja", ["TM"] = "Turkmenistan", ["TV"] = "Tuvalu", ["UG"] = "Uganda", ["UA"] = "Ukraina",
            ["AE"] = "Zjednoczone Emiraty Arabskie", ["GB"] = "Wielka Brytania", ["US"] = "Stany Zjednoczone", ["UY"] = "Urugwaj",
            ["UZ"] = "Uzbekistan", ["VU"] = "Vanuatu", ["VA"] = "Watykan", ["VE"] = "Wenezuela", ["VN"] = "Wietnam",
            ["YE"] = "Jemen", ["ZM"] = "Zambia", ["ZW"] = "Zimbabwe",
        };

    /// <summary>Returns the Polish Wikipedia page title for a country ISO code.</summary>
    public static string Get(string isoCode) =>
        ByIsoCode.TryGetValue(isoCode, out var title) ? title : isoCode;
}
