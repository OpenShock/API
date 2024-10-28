using OpenShock.Common.Authentication;
using OpenShock.Common.Models;
using System.Linq.Expressions;

namespace OpenShock.Common;

public static class IQueryableExtensions
{
    public static IQueryable<TEntity> WhereIsUserOrRank<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, Guid userId, RankType rank)
    {
        // Create a parameter for TEntity (entity)
        var entityParameter = Expression.Parameter(typeof(TEntity), "entity");

        // Access the user from the navigation selector
        var userExpression = Expression.Invoke(navigationSelector, entityParameter);

        // Build expressions to check user ID and rank
        var userIdCheck = Expression.Equal(
            Expression.Property(userExpression, nameof(OpenShockDb.User.Id)),
            Expression.Constant(userId)
        );

        var userRankCheck = Expression.GreaterThanOrEqual(
            Expression.Property(userExpression, nameof(OpenShockDb.User.Rank)),
            Expression.Constant(rank)
        );

        // Combine the checks using "AND" (userId == userId && userRank >= requiredRank)
        var combinedCheck = Expression.AndAlso(userIdCheck, userRankCheck);

        // Build the lambda expression
        var predicate = Expression.Lambda<Func<TEntity, bool>>(combinedCheck, entityParameter);

        // Apply the predicate to the source IQueryable
        return source.Where(predicate);
    }

    public static IQueryable<TEntity> WhereIsUserOrRank<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, OpenShockDb.User user, RankType rank)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.Id, rank);
    }

    public static IQueryable<TEntity> WhereIsUserOrRank<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, LinkUser user, RankType rank)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.DbUser.Id, rank);
    }

    public static IQueryable<TEntity> WhereIsUserOrAdmin<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, OpenShockDb.User user)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.Id, RankType.Admin);
    }

    public static IQueryable<TEntity> WhereIsUserOrAdmin<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, OpenShockDb.User>> navigationSelector, LinkUser user)
    {
        return WhereIsUserOrRank(source, navigationSelector, user.DbUser.Id, RankType.Admin);
    }
}
