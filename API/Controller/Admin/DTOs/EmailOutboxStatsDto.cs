namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Count of email outbox rows in each delivery state, for the admin mail-queue overview tiles.
/// </summary>
public sealed class EmailOutboxStatsDto
{
    public required long Pending { get; init; }
    public required long Sending { get; init; }
    public required long Sent { get; init; }
    public required long Failed { get; init; }
    public required long Skipped { get; init; }
    public required long Total { get; init; }
}
