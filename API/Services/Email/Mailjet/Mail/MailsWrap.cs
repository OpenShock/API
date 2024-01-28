namespace OpenShock.API.Services.Email.Mailjet.Mail;

public class MailsWrap
{
    public required IEnumerable<Mail.MailBase> Messages { get; set; }
}