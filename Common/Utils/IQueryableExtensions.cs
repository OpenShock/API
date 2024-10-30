namespace OpenShock.Common.Utils;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string filterQuery) where T : class
    {
        var filter = ExpressionBuilder.GetFilterExpression<T>(filterQuery);

        if (filter != null)
        {
            query = query.Where(filter);
        }
        
        return query;
    }

    public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {
        return query;
    }
}
