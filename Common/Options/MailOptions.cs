using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Options;

/// <summary>
/// Mail provider selection. Delivery itself only concerns the Cron host, but the API needs the type
/// too: with <see cref="MailType.None"/> no activation/verification link is ever delivered, so the
/// flows that would depend on one must not be started.
/// </summary>
public sealed class MailOptions
{
    public const string SectionName = "OpenShock:Mail";
    public const string SenderSectionName = SectionName + ":Sender";

    [Required]
    public required MailType Type { get; init; }

    public bool IsEnabled => Type != MailType.None;

    public enum MailType
    {
        Mailjet = 0,
        Smtp = 1,
        None = 2
    }
}