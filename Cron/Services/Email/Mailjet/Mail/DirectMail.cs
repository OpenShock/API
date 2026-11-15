namespace OpenShock.Cron.Services.Email.Mailjet.Mail;

public sealed class DirectMail
{
    public required Contact From { get; init; }
    public required Contact[] To { get; init; }
    public required string Subject { get; init; }
    public string? HTMLPart { get; init; }
}
