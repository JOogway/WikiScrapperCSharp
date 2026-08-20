namespace WikiScrapper.Services;

/// <summary>
/// Settings for the Wikipedia REST clients and sync throughput.
/// Bound from the <c>Wikipedia</c> configuration section.
/// </summary>
public sealed class WikipediaClientOptions
{
    public const string SectionName = "Wikipedia";

    public const string EnClientName = "wikipedia-en";
    public const string PlClientName = "wikipedia-pl";

    /// <summary>Default maximum parallel Wikipedia fetches during sync.</summary>
    public const int DefaultMaxConcurrency = 8;

    /// <summary>Hard upper bound for configured concurrency.</summary>
    public const int AbsoluteMaxConcurrency = 32;

    /// <summary>English Wikipedia REST v1 base URL.</summary>
    public string EnBaseUrl { get; set; } = "https://en.wikipedia.org/api/rest_v1/";

    /// <summary>Polish Wikipedia REST v1 base URL.</summary>
    public string PlBaseUrl { get; set; } = "https://pl.wikipedia.org/api/rest_v1/";

    /// <summary>
    /// Maximum number of Wikipedia page fetches that may run at the same time.
    /// </summary>
    public int MaxConcurrency { get; set; } = DefaultMaxConcurrency;
}
