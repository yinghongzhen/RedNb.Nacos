using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model;

/// <summary>
/// Generic paged result for list operations.
/// </summary>
/// <typeparam name="T">Type of items in the list.</typeparam>
public class PageResult<T>
{
    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Items in the current page.
    /// </summary>
    [JsonPropertyName("pageItems")]
    public List<T> PageItems { get; set; } = new();

    /// <summary>
    /// Whether there are more pages.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Creates an empty page result.
    /// </summary>
    public static PageResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PageResult<T>
        {
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize,
            PageItems = new List<T>()
        };
    }

    /// <summary>
    /// Creates a page result from items.
    /// </summary>
    public static PageResult<T> FromItems(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PageResult<T>
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            PageItems = items
        };
    }
}
