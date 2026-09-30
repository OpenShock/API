namespace OpenShock.Common.Utils.Pagination;

/// <summary>
/// Page of results for a paginated list endpoint.
/// </summary>
/// <typeparam name="T">Item shape</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Items on the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// 1-based current page index.
    /// </summary>
    public required uint Page { get; set; }

    /// <summary>
    /// Page size used to produce this result.
    /// </summary>
    public required uint PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages matching the query.
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages. Always at least 1, even when <see cref="TotalCount"/> is zero.
    /// </summary>
    public uint TotalPages => TotalCount <= 0
        ? 1u
        : (uint)Math.Ceiling(TotalCount / (double)PageSize);
}
