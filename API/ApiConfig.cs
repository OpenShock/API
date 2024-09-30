using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.ServicesCommon.Config;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API;

public sealed class ApiConfig : BaseConfig
{
    [Required] public required FrontendConfig Frontend { get; init; }
    [Required] public required MailConfig Mail { get; init; }
    [Required] public required TurnstileConfig Turnstile { get; init; }

    public sealed class TurnstileConfig
    {
        [Required] public required bool Enabled { get; init; }
        public string? SecretKey { get; init; }
        public string? SiteKey { get; init; }
    }

    public sealed class MailConfig
    {
        [Required] public required MailType Type { get; init; }
        [Required] public required Contact Sender { get; init; }
        public MailjetConfig? Mailjet { get; init; }
        public SmtpConfig? Smtp { get; init; }

        public enum MailType
        {
            Mailjet = 0,
            Smtp = 1
        }

        public sealed class SmtpConfig
        {
            [Required(AllowEmptyStrings = false)] public required string Host { get; init; }
            public int Port { get; init; } = 587;
            public string Username { get; init; } = string.Empty;
            public string Password { get; init; } = string.Empty;
            public bool EnableSsl { get; init; } = true;
            public bool VerifyCertificate { get; init; } = true;
        }

        public sealed class MailjetConfig
        {
            [Required(AllowEmptyStrings = false)] public required string Key { get; init; }
            [Required(AllowEmptyStrings = false)] public required string Secret { get; init; }

            [Required] public required TemplateConfig Template { get; init; }

            public sealed class TemplateConfig
            {
                [Required] public required ulong PasswordReset { get; init; }
                [Required] public required ulong PasswordResetComplete { get; init; }
                [Required] public required ulong VerifyEmail { get; init; }
                [Required] public required ulong VerifyEmailComplete { get; init; }
            }
        }
    }



}