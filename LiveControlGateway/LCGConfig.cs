// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Utils;

namespace OpenShock.LiveControlGateway;

public class LCGConfig
{
    [Required(AllowEmptyStrings = false)] public required string Fqdn { get; set; }
    
    [Alpha2CountryCode]
    public required string CountryCode { get; set; }
    [Required(AllowEmptyStrings = false)] public required string Db { get; init; }
    public required RedisConfig Redis { get; init; }

    public class RedisConfig
    {
        [Required(AllowEmptyStrings = false)] public required string Host { get; init; }
        [Required(AllowEmptyStrings = false)] public required string User { get; init; }
        [Required(AllowEmptyStrings = false)] public required string Password { get; init; }
        public required ushort Port { get; init; }
    }
}