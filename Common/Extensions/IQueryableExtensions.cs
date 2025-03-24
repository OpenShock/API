using System.Linq.Expressions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Query;

namespace OpenShock.Common.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<TEntity> WhereUserIdMatches<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, User>> userNavigation, Guid userId)
    {
        var userIdConstant = Expression.Constant(userId);
        var userIdProperty = Expression.Property(userNavigation.Body, nameof(User.Id));

        var comparison = Expression.Equal(userIdProperty, userIdConstant);

        var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, userNavigation.Parameters[0]);

        return source.Where(lambda);
    }

    public static IQueryable<TEntity> WhereIsUserOrPrivileged<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, User>> userNavigation, User user)
    {
        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return source;
        }

        return WhereUserIdMatches(source, userNavigation, user.Id);
    }

    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string filterQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(filterQuery)) return query;
        
        return query.Where(DBExpressionBuilder.GetFilterExpression<T>(filterQuery));
    }


    public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(orderbyQuery))
            return query;

        // Split the query into property name and direction
        if (orderbyQuery.Split(' ') is not [string propOrFieldName, string direction])
            throw new ArgumentException($"{nameof(orderbyQuery)} should contain a property name and a direction (asc or desc).", nameof(orderbyQuery));

        var entityType = typeof(T);

        var parameterExpr = Expression.Parameter(entityType, "x");
        var memberExpr = Expression.PropertyOrField(parameterExpr, propOrFieldName);
        var lambda = Expression.Lambda(memberExpr, parameterExpr);

        // Use the helper method to build the expression
        var orderByCall = direction.ToLower() switch
        {
            "asc" => DBExpressionBuilderUtils.BuildOrderBy(query, lambda, descending: false),
            "desc" => DBExpressionBuilderUtils.BuildOrderBy(query, lambda, descending: true),
            _ => throw new ArgumentException("Direction must be 'asc' or 'desc'.", nameof(orderbyQuery))
        };

        return query.Provider.CreateQuery<T>(orderByCall);
    }
}
