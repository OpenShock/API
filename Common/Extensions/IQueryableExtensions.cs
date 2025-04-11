using System.Linq.Expressions;
using System.Reflection;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(filterQuery, nameof(filterQuery));

        return query.Where(DBExpressionBuilder.GetFilterExpression<T>(filterQuery));
    }

    public static IOrderedQueryable<T> ApplyOrderBy<T>(IQueryable<T> query, string orderbyQuery) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderbyQuery, nameof(orderbyQuery));

        return OrderByQueryBuilder.ApplyOrderBy(query, orderbyQuery);
    }
}
