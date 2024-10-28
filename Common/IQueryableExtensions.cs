using OpenShock.Common.Authentication;
using OpenShock.Common.Models;
using System.Linq.Expressions;

namespace OpenShock.Common;

public static class IQueryableExtensions
{
    private static Expression<Func<TEntity, bool>> EntityPropExpr<TEntity>(Expression navigationSelector, Func<InvocationExpression, BinaryExpression> binaryExpressionGetter)
    {
        // Create a parameter for TEntity (entity)
        var entityParameter = Expression.Parameter(typeof(TEntity), "entity");

        // Access the prop from the navigation selector
        var entityExpression = Expression.Invoke(navigationSelector, entityParameter);

        var binaryExpression = binaryExpressionGetter(entityExpression);

        // Build the lambda expression
        return Expression.Lambda<Func<TEntity, bool>>(binaryExpression, entityParameter);
    }

    private static BinaryExpression UserIdMatchesExpr(InvocationExpression userExpression, Guid userId)
    {
        return Expression.Equal(
            Expression.Property(userExpression, nameof(OpenShockDb.User.Id)),
            Expression.Constant(userId)
        );
    }

    private static Expression<Func<TEntity, bool>> IsUserMatchExpr<TEntity>(Expression navigationSelector, Guid userId)
    {
        return EntityPropExpr<TEntity>(navigationSelector, user => UserIdMatchesExpr(user, userId));
    }

    public static IQueryable<TEntity> WhereIsUser<TEntity>(this IQueryable<TEntity> source, Expression navigationSelector, Guid userId)
    {
        return source.Where(IsUserMatchExpr<TEntity>(navigationSelector, userId));
    }

    public static IQueryable<TEntity> WhereIsUserOrRank<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, OpenShockDb.User user, RankType rank)
    {
        if (user.Rank >= rank)
        {
            return source;
        }

        return WhereIsUser(source, navigationSelector, user.Id);
    }

    public static IQueryable<TEntity> WhereIsUserOrRank<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, LinkUser user, RankType rank)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.DbUser, rank);
    }

    public static IQueryable<TEntity> WhereIsUserOrAdmin<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, OpenShockDb.User user)
    {
        return WhereIsUserOrRank(source, navigationSelector, user, RankType.Admin);
    }

    public static IQueryable<TEntity> WhereIsUserOrAdmin<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, LinkUser user)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.DbUser, RankType.Admin);
    }
}
