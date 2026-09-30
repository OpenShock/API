using Microsoft.AspNetCore.Mvc;

namespace OpenShock.Common.Utils.Pagination;

/// <summary>
/// Common query parameters for paginated list endpoints. Bind with
/// <c>[FromQuery] PaginationQuery query</c> on the action.
/// </summary>
public sealed class PaginationQuery
{
    /// <summary>
    /// Default page size when the client does not specify one.
    /// </summary>
    public const uint DefaultPageSize = 25;

    /// <summary>
    /// Upper bound for <see cref="PageSize"/> regardless of what the client sends.
    /// </summary>
    public const uint MaxPageSize = 200;

    private uint _page = 1;
    private uint _pageSize = DefaultPageSize;

    /// <summary>
    /// 1-based page index. Values below 1 are clamped to 1.
    /// </summary>
    [FromQuery(Name = "page")]
    public uint Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page. Clamped to <c>[1, <see cref="MaxPageSize"/>]</c>.
    /// </summary>
    [FromQuery(Name = "pageSize")]
    public uint PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1) _pageSize = 1;
            else if (value > MaxPageSize) _pageSize = MaxPageSize;
            else _pageSize = value;
        }
    }

    private string? _search;

    /// <summary>
    /// Optional free-text search term. Whitespace is trimmed; empty becomes null.
    /// Endpoints decide which fields the term is matched against.
    /// </summary>
    [FromQuery(Name = "search")]
    public string? Search
    {
        get => _search;
        set => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string? _sort;

    /// <summary>
    /// Optional sort key. Endpoints register the allowed keys and translate them to
    /// the underlying column(s). Unknown values fall back to the endpoint default.
    /// </summary>
    [FromQuery(Name = "sort")]
    public string? Sort
    {
        get => _sort;
        set => _sort = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Sort direction. Defaults to <see cref="SortDirection.Desc"/> when omitted, since list
    /// endpoints almost always want "newest/highest first" as the default.
    /// </summary>
    [FromQuery(Name = "sortDir")]
    public SortDirection SortDir { get; set; } = SortDirection.Desc;

    /// <summary>
    /// Number of items to skip based on <see cref="Page"/> and <see cref="PageSize"/>.
    /// </summary>
    public uint GetSkip() => (Page - 1) * PageSize;
}

/// <summary>
/// Sort direction for list endpoints.
/// </summary>
public enum SortDirection
{
    Asc,
    Desc,
}
