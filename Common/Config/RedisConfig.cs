namespace OpenShock.Common.Config;

public sealed class RedisConfig
{
    public required string Conn { get; set; }
    public required string Host { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public short Port { get; init; } = 6379;
}