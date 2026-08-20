using System.Text.Json.Serialization;

namespace WikiScrapper.Services;

/// <summary>
/// JSON shape of the Wikipedia REST API "page summary" response
/// (only the fields the application consumes).
/// </summary>
internal sealed class WikipediaSummaryResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("extract")]
    public string? Extract { get; set; }

    [JsonPropertyName("content_urls")]
    public ContentUrls? Urls { get; set; }

    internal sealed class ContentUrls
    {
        [JsonPropertyName("desktop")]
        public UrlSet? Desktop { get; set; }
    }

    internal sealed class UrlSet
    {
        [JsonPropertyName("page")]
        public string? Page { get; set; }
    }
}
