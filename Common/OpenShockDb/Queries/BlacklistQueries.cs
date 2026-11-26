using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

public static class BlacklistQueries
{
    /// <summary>
    /// The blacklist entries <paramref name="username"/> runs into, ignoring case: exact entries equal to it and
    /// contains entries found anywhere inside it.
    /// </summary>
    /// <remarks>
    /// The value column's own collation makes the exact comparison ignore case. The substring search folds case
    /// itself under a deterministic collation instead of using LIKE, so an underscore or percent sign in an
    /// entry is matched literally.
    /// </remarks>
    public static IQueryable<UserNameBlacklist> Matching(this DbSet<UserNameBlacklist> blacklist, string username)
    {
        return blacklist.FromSql(
            $"""
             SELECT * FROM user_name_blacklist
             WHERE (match_type = 'exact' AND value = {username})
                OR (match_type = 'contains' AND strpos(lower({username} COLLATE "und-x-icu"), lower(value COLLATE "und-x-icu")) > 0)
             """);
    }
}
