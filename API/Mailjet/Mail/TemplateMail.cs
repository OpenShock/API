namespace OpenShock.API.Mailjet.Mail;

public class TemplateMail : MailBase
{
    public bool TemplateLanguage { get; set; } = true;
    public required long TemplateId { get; set; }
    public new required Dictionary<string, string> Variables { get; set; } = new();
}