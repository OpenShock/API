namespace OpenShock.Common.Models;

public static class RankUtils
{
    public static bool IsAllowed(this RankType userRank, RankType rankNeeded) => userRank >= rankNeeded;
}