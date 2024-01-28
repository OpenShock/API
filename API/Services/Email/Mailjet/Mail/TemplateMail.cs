namespace OpenShock.API.Services.Email.Mailjet.Mail;

public class TemplateMail : MailBase
{
    public bool TemplateLanguage { get; set; } = true;
    public required ulong TemplateId { get; set; }
    public new required Dictionary<string, string> Variables { get; set; } = new();
}