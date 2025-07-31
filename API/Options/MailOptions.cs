using System.ComponentModel.DataAnnotations;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Options;

public sealed class MailOptions
{
    public const string SectionName = "OpenShock:Mail";
    public const string SenderSectionName = SectionName + ":Sender";

    [Required]
    public required MailType Type { get; init; }

    public enum MailType
    {
        Mailjet = 0,
        Smtp = 1,
        None = 2
    }

    public sealed class MailSenderContact : Contact;
}
