namespace OpenShock.API.Mailjet.Mail;

public abstract class MailBase
{
    public required Contact From  { get; set; }
    public required IEnumerable<Contact> To { get; set; }
    public required string Subject { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}