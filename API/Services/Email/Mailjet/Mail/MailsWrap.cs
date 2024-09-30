namespace OpenShock.API.Services.Email.Mailjet.Mail;

public sealed class MailsWrap
{
    public required IEnumerable<Mail.MailBase> Messages { get; set; }
}