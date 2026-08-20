namespace WikiScrapper.Domain;

/// <summary>Aggregate fetch statistics for voivodeships or countries.</summary>
public record FetchStats(int Total, int Fetched, DateTime? LastFetchedAt);
