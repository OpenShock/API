namespace OpenShock.API;

public class ApiConfig
{
    public required Uri FrontendBaseUrl { get; init; }
    public required Uri CookieDomain { get; init; }
    public required string Db { get; init; }
    public required bool SkipDbMigration { get; init; }
    public required RedisConfig Redis { get; init; }
    public required MailjetConfig Mailjet { get; init; }
    
    public class RedisConfig
    {
        public required string Host { get; init; }
        public required string User { get; init; }
        public required string Password { get; init; }
        public required ushort Port { get; init; }
    }
    
    public class MailjetConfig
    {
        public required string Key { get; init; }
        public required string Secret { get; init; }
    }
}