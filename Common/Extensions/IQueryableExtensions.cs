using System.Linq.Expressions;
using System.Reflection;
using OpenShock.Common.Query;

namespace OpenShock.Common.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string filterQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(filterQuery)) return query;
        
        return query.Where(DBExpressionBuilder.GetFilterExpression<T>(filterQuery));
    }
    
    private static MethodInfo[] PublicQueryableMethods => typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public);
    private static readonly MethodInfo OrderByAscending = PublicQueryableMethods.Single(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
    private static readonly MethodInfo OrderByDescending = PublicQueryableMethods.Single(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);

    public static IOrderedQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(orderbyQuery))
            throw new ArgumentException($"{nameof(orderbyQuery)} cannot be null or empty.", nameof(orderbyQuery));

        // Split the query into property name and direction
        if (orderbyQuery.Split(' ') is not [string propOrFieldName, string direction])
            throw new ArgumentException($"{nameof(orderbyQuery)} should contain a property name and an optional direction (asc or desc).", nameof(orderbyQuery));
        
        var entityType = typeof(T);
        
        var (memberInfo, memberType) = DBExpressionBuilderUtils.GetPropertyOrField(entityType, propOrFieldName);

        var parameterExpr = Expression.Parameter(entityType, "x");
        var memberExpr = Expression.MakeMemberAccess(parameterExpr, memberInfo);
        var lambda = Expression.Lambda(memberExpr, parameterExpr);

        var methodInfo = direction switch
        {
            "asc" => OrderByAscending,
            "desc" => OrderByDescending,
            _ => throw new ArgumentException(),
        };
        
        var method = methodInfo.MakeGenericMethod(entityType, memberType);

        // Invoke the method on the query with the key selector and return the ordered query
        return (IOrderedQueryable<T>)method.Invoke(null, [query, lambda])!;
    }
}
