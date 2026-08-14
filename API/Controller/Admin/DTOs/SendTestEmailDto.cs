using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Request to enqueue a preview/test email of a chosen type to a chosen address. The message is
/// delivered by the Cron pipeline with placeholder data and a dummy link (no token, no request row).
/// </summary>
public sealed class SendTestEmailDto
{
    /// <summary>Which template to preview.</summary>
    public required EmailType Type { get; set; }

    /// <summary>Destination address for the test email.</summary>
    [OpenShock.Common.DataAnnotations.EmailAddress(true)]
    public required string Recipient { get; set; }

    /// <summary>Optional display name for the recipient.</summary>
    public string? RecipientName { get; set; }
}
