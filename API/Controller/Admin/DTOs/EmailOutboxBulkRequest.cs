using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// A set of email outbox message ids to apply a bulk requeue/cancel/delete to.
/// </summary>
public sealed class EmailOutboxBulkRequest
{
    [Required]
    [MaxLength(1000)]
    public required Guid[] Ids { get; set; }
}
