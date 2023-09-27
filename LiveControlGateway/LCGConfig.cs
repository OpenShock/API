// ReSharper disable InconsistentNaming
namespace ShockLink.LiveControlGateway;

public class LCGConfig
{
    public required string Db { get; init; }
    public required RedisConfig Redis { get; init; }

    public class RedisConfig
    {
        public required string Host { get; init; }
        public required string User { get; init; }
        public required string Password { get; init; }
        public required ushort Port { get; init; }
    }
}