using System.Diagnostics;
using System.Linq.Expressions;

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

    public static IOrderedQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(orderbyQuery))
            throw new ArgumentException($"{nameof(orderbyQuery)} cannot be null or empty.", nameof(orderbyQuery));

        // Split the query into property name and direction
        if (orderbyQuery.Split(' ') is not [string propOrFieldName, string direction])
            throw new ArgumentException($"{nameof(orderbyQuery)} should contain a property name and an optional direction (asc or desc).", nameof(orderbyQuery));
        
        var entityType = typeof(T);
        
        var memberInfo = ExpressionBuilder.GetPropertyOrField(entityType, propOrFieldName);
        if (memberInfo == null)
            throw new ExpressionBuilder.ExpressionException($"'{propOrFieldName}' is not a valid property");

        var parameterExpr = Expression.Parameter(entityType, "x");
        var memberExpr = Expression.MakeMemberAccess(parameterExpr, memberInfo);
        var lambda = Expression.Lambda(memberExpr, parameterExpr);

        var methodName = direction switch
        {
            "asc" => "OrderBy",
            "desc" => "OrderByDescending",
            _ => throw new ArgumentException(),
        };

        var memberType = ExpressionBuilder.GetPropertyOrFieldType(memberInfo);
        if (memberType == null)
            throw new ExpressionBuilder.ExpressionException("Unknown error occured");
        
        // Get the appropriate Queryable method (OrderBy or OrderByDescending)
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType, memberType);

        // Invoke the method on the query with the key selector and return the ordered query
        return (IOrderedQueryable<T>)method.Invoke(null, [query, lambda])!;
    }
}
