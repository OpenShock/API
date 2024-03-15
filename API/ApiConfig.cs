using System.ComponentModel.DataAnnotations;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API;

public class ApiConfig
{
    [Required] public required Uri FrontendBaseUrl { get; init; }
    [Required(AllowEmptyStrings = false)] public required string CookieDomain { get; init; }
    [Required] public required DbConfig Db { get; init; }
    [Required] public required RedisConfig Redis { get; init; }
    [Required] public required MailConfig Mail { get; init; }
    [Required] public required TurnstileConfig Turnstile { get; init; }
    
    public sealed class TurnstileConfig
    {
        [Required] public required bool Enabled { get; init; }
        [Required] public required string? SecretKey { get; init; }
        [Required] public required string? SiteKey { get; init; }
    }

    public sealed class MailConfig
    {
        [Required] public required MailType Type { get; init; }
        [Required] public required Contact Sender { get; init; }
        public required MailjetConfig? Mailjet { get; init; }
        public required SmtpConfig? Smtp { get; init; }

        public enum MailType
        {
            Mailjet = 0,
            Smtp = 1
        }

        public sealed class SmtpConfig
        {
            [Required(AllowEmptyStrings = false)] public required string Host { get; init; }
            public required int Port { get; init; } = 587;
            public required string Username { get; init; } = string.Empty;
            public required string Password { get; init; } = string.Empty;
            public required bool EnableSsl { get; init; } = true;
            public required bool VerifyCertificate { get; init; } = true;
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
                [Required] public required ulong ActivateAccount { get; init; }
                [Required] public required ulong ActivateAccountComplete { get; init; }
            }
        }
    }

    public sealed class DbConfig
    {
        [Required(AllowEmptyStrings = true)] public required string Conn { get; init; }
        public required bool SkipMigration { get; init; } = false;
        public required bool Debug { get; init; } = false;
    }

    public sealed class RedisConfig
    {
        public required string Host { get; init; } = string.Empty;
        public required string User { get; init; } = string.Empty;
        public required string Password { get; init; } = string.Empty;
        public required ushort Port { get; init; } = 6379;
    }
}