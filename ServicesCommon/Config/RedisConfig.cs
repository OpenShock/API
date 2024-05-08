using System.ComponentModel.DataAnnotations;

namespace OpenShock.ServicesCommon.Config;

public sealed class RedisConfig
{
    [Required(AllowEmptyStrings = false)] public required string Host { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public short Port { get; init; } = 6379;
}