namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Count of email outbox rows in each delivery state, for the admin mail-queue overview tiles.
/// </summary>
public sealed class EmailOutboxStatsDto
{
    public required long Pending { get; set; }
    public required long Sending { get; set; }
    public required long Sent { get; set; }
    public required long Failed { get; set; }
    public required long Skipped { get; set; }
    public required long Total { get; set; }
}
