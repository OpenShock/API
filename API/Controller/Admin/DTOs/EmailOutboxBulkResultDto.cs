namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Outcome of a bulk email outbox operation: how many ids were requested and how many rows were
/// actually affected (rows that were missing or in an ineligible state are silently not counted).
/// </summary>
public sealed class EmailOutboxBulkResultDto
{
    public required int Requested { get; set; }
    public required int Affected { get; set; }
}
