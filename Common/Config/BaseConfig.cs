using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Config;

public class BaseConfig
{
    [Required] public required DbConfig Db { get; init; }
    [Required] public required RedisConfig Redis { get; init; }

    public MetricsConfig Metrics { get; init; } = new();
}