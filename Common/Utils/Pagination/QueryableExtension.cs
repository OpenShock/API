using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.Utils.Pagination;

/// <summary>
/// Applies an ordering to <paramref name="source"/>. <paramref name="descending"/> indicates
/// whether the caller-requested direction is descending; the implementation owns the
/// actual column(s) and is responsible for honouring that.
/// </summary>
public delegate IOrderedQueryable<T> SortFunc<T>(IQueryable<T> source, bool descending);

/// <summary>
/// Reusable helpers for applying <see cref="PaginationQuery"/> to an <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies the sort requested by <paramref name="pagination"/> using the supplied
    /// <paramref name="sorters"/> map. Falls back to <paramref name="defaultSort"/> when
    /// the client did not specify a sort key or sent an unknown one.
    ///
    /// Callers should chain a stable tiebreaker (e.g. <c>.ThenBy(x =&gt; x.Id)</c>) after
    /// this call so pages remain deterministic when the primary sort has ties.
    /// </summary>
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        PaginationQuery pagination,
        IReadOnlyDictionary<string, SortFunc<T>> sorters,
        string defaultSort)
    {
        var key = pagination.Sort is { } requested && sorters.ContainsKey(requested)
            ? requested
            : defaultSort;
        var descending = pagination.SortDir == SortDirection.Desc;
        return sorters[key](source, descending);
    }

    /// <summary>
    /// Executes <paramref name="query"/> as a paginated request: runs one COUNT and one
    /// SELECT with <c>Skip</c>/<c>Take</c> based on <paramref name="pagination"/>.
    ///
    /// Apply search filters and ordering on <paramref name="query"/> before calling this —
    /// pagination without a stable order produces non-deterministic pages.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((int)pagination.GetSkip())
            .Take((int)pagination.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Variant of <see cref="ToPagedResultAsync{T}"/> that counts on the source query and
    /// applies <paramref name="projection"/> only to the page slice. Useful when the
    /// projection is expensive or pulls in joins that the count does not need.
    /// </summary>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TResult>> projection,
        PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((int)pagination.GetSkip())
            .Take((int)pagination.PageSize)
            .Select(projection)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }
}