using System.Linq.Expressions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

using OpenShock.Internal.DynamicLinq.Extensions;

namespace OpenShock.Common.Extensions;

// ApplyFilter/ApplyOrderBy now live in OpenShock.Internal.DynamicLinq (OpenShock.Internal.DynamicLinq.Extensions).
// These two helpers depend on the User/RoleType domain model and stay local.
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
}
