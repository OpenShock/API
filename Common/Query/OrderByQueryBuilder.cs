using System.Linq.Expressions;
using System.Reflection;

namespace OpenShock.Common.Query;

public static class OrderByQueryBuilder
{
    private static MethodInfo[] PublicQueryableMethods => typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public);
    private static readonly MethodInfo OrderByAscendingMethodInfo = PublicQueryableMethods.Single(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
    private static readonly MethodInfo OrderByDescendingMethodInfo = PublicQueryableMethods.Single(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
    private static readonly MethodInfo OrderThenByAscendingMethodInfo = PublicQueryableMethods.Single(m => m.Name == "ThenBy" && m.GetParameters().Length == 2);
    private static readonly MethodInfo OrderThenByDescendingMethodInfo = PublicQueryableMethods.Single(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 2);

    private record struct OrderByItem(string Name, bool Descending);

    private static OrderByItem ParseOrderByPart(string str)
    {
        var parts = str.Split(' ');
        if (parts.Length == 1)
        {
            return new OrderByItem(str, false);
        }

        if (parts.Length == 2)
        {
            bool descending = parts[1].ToLower() switch
            {
                "asc" => false,
                "desc" => true,
                _ => throw new InvalidOperationException("Direction if specified must be 'asc' or 'desc'."),
            };

            return new OrderByItem(parts[0], descending);
        }

        throw new InvalidOperationException("Invalid orderby query.");
    }

    private static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, LambdaExpression keySelector, bool descending = false)
    {
        var method = (descending ? OrderByDescendingMethodInfo : OrderByAscendingMethodInfo)
            .MakeGenericMethod(typeof(T), keySelector.Body.Type);

        var call = Expression.Call(null, method, source.Expression, Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery(call);
    }

    private static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, LambdaExpression keySelector, bool descending = false)
    {
        var method = (descending ? OrderThenByDescendingMethodInfo : OrderThenByAscendingMethodInfo)
            .MakeGenericMethod(typeof(T), keySelector.Body.Type);

        var call = Expression.Call(null, method, source.Expression, Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery(call);
    }

    public static IOrderedQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(orderbyQuery);
        
        var parts = orderbyQuery.Split(',');

        var parsed = ParseOrderByPart(parts[0]);
        var orderedQuery = query.OrderBy(DBExpressionBuilderUtils.CreatePropertyOrFieldAccessorExpression<T>(parsed.Name), parsed.Descending);

        for (int i = 1; i < parts.Length; ++i)
        {
            parsed = ParseOrderByPart(parts[i]);
            orderedQuery = orderedQuery.ThenBy(DBExpressionBuilderUtils.CreatePropertyOrFieldAccessorExpression<T>(parsed.Name), parsed.Descending);
        }

        return orderedQuery;
    }
}
