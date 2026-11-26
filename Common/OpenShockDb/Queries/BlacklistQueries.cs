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
    /// entry is matched literally. It uses casefold rather than lower, since lower is context sensitive (a final
    /// sigma lowercases to ς but to σ mid-word), which would make a folded entry miss the same text inside a
    /// longer name. Both sides are then normalized to NFC, so an accent written as one precomposed character
    /// matches the same accent written as a base letter plus a combining mark.
    /// </remarks>
    public static IQueryable<UserNameBlacklist> Matching(this DbSet<UserNameBlacklist> blacklist, string username)
    {
        return blacklist.FromSql(
            $"""
             SELECT * FROM user_name_blacklist
             WHERE (match_type = 'exact' AND value = {username})
                OR (match_type = 'contains' AND strpos(normalize(casefold({username} COLLATE "und-x-icu"), NFC), normalize(casefold(value COLLATE "und-x-icu"), NFC)) > 0)
             """);
    }
}
