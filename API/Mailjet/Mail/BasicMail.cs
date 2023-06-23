namespace ShockLink.API.Mailjet.Mail;

public class BasicMail : MailBase
{
    public string? TextPart { get; set; }
    public string? HTMLPart { get; set; }
}