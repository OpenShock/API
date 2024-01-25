using System.ComponentModel.DataAnnotations;

namespace OpenShock.API;

public class ApiConfig
{
    [Required]
    public required Uri FrontendBaseUrl { get; init; }
    [Required(AllowEmptyStrings = false)]
    public required string CookieDomain { get; init; }
    [Required]
    public required DbConfig Db { get; init; }
    public required RedisConfig Redis { get; init; }
    public required MailjetConfig? Mailjet { get; init; } = null;
    
    public sealed class DbConfig
    {
        [Required(AllowEmptyStrings = true)]
        public required string Conn { get; init; }
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
    
    public class MailjetConfig
    {
        public required string Key { get; init; }
        public required string Secret { get; init; }
    }
}