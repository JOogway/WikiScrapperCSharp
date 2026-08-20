namespace WikiScrapper.Domain.Dtos;

/// <summary>
/// A single page of a larger result set.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">Items on the current page.</param>
/// <param name="TotalCount">Total number of items matching the query across all pages.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Total number of pages for the query.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNext => Page < TotalPages;
}
