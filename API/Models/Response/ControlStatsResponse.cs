using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class ControlStatsResponse
{
    /// <summary>Activity on shockers you own.</summary>
    public required ReceivedControlStats Received { get; init; }

    /// <summary>Activity you sent to shockers, your own included.</summary>
    public required GivenControlStats Given { get; init; }
}

public sealed class ReceivedControlStats
{
    public required ControlStatTotals[] Totals { get; init; }
    public required ShockerControlStats[] Shockers { get; init; }
    public required UserControlStats[] Controllers { get; init; }
    public required DailyControlStats[] Days { get; init; }
}

public sealed class GivenControlStats
{
    public required ControlStatTotals[] Totals { get; init; }
    public required UserControlStats[] Owners { get; init; }
}

/// <summary>Aggregated activity of one control type, split by whether it was live control.</summary>
public sealed class ControlStatTotals
{
    public required ControlType Type { get; init; }
    public required bool Live { get; init; }

    /// <summary>Number of controls, for live control the number of continuous bursts.</summary>
    public required long Count { get; init; }

    public required long DurationMs { get; init; }

    /// <summary>Average intensity weighted by duration.</summary>
    public required double AverageIntensity { get; init; }

    public required byte MaxIntensity { get; init; }
}

public sealed class ShockerControlStats
{
    /// <summary>Null for shockers that have since been deleted, these are combined into one entry.</summary>
    public required Guid? ShockerId { get; init; }
    public required string? ShockerName { get; init; }
    public required ControlStatTotals[] Stats { get; init; }
}

public sealed class UserControlStats
{
    /// <summary>Null for guests (public shares).</summary>
    public required Guid? UserId { get; init; }

    /// <summary>Null for guests and deleted users.</summary>
    public required BasicUserInfo? User { get; init; }

    public required ControlStatTotals[] Stats { get; init; }
}

public sealed class DailyControlStats
{
    /// <summary>Day in the requested time zone.</summary>
    public required DateOnly Date { get; init; }
    public required ControlStatTotals[] Stats { get; init; }
}
