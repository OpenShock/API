namespace OpenShock.Common.Services.Email.Mailjet.Mail;

public sealed class DirectMail
{
    public required Contact From { get; set; }
    public required Contact[] To { get; set; }
    public required string Subject { get; set; }
    public string? HTMLPart { get; set; }
}