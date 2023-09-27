namespace OpenShock.API.Mailjet;

public interface IMailjetClient
{
    public Task SendMail(Mail.MailBase templateMail);
    public Task SendMails(IEnumerable<Mail.MailBase> mails);
}