using System.ComponentModel.DataAnnotations;

namespace OpenShock.ServicesCommon.Config;

public class BaseConfig
{
    [Required] public required DbConfig Db { get; init; }
    [Required] public required RedisConfig Redis { get; init; }
}