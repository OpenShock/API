using Microsoft.Extensions.Options;
using OpenShock.API.Services.Email.Mailjet.Mail;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Options;

public sealed class MailOptions
{
    public const string SectionName = "OpenShock:Mail";
    public const string SenderSectionName = SectionName + ":Sender";
    public const string SenderOptionName = "EmailSender";

    [Required]
    public required MailType Type { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required Contact Sender { get; init; }

    [ValidateObjectMembers]
    public MailJetOptions? Mailjet {  get; init; }

    [ValidateObjectMembers]
    public SmtpOptions? Smtp { get; init; }

    public enum MailType
    {
        Mailjet = 0,
        Smtp = 1
    }
}
