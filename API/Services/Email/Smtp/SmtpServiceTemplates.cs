namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpServiceTemplates
{
    public required SmtpTemplate PasswordReset { get; set; }
    public required SmtpTemplate EmailVerification { get; set; }
}