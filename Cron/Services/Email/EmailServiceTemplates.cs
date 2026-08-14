namespace OpenShock.Cron.Services.Email;

public sealed class EmailServiceTemplates
{
    public required EmailTemplate AccountActivation { get; init; }
    public required EmailTemplate PasswordReset { get; init; }
    public required EmailTemplate EmailVerification { get; init; }
    public required EmailTemplate EmailChangeNotice { get; init; }
}
