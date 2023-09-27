// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace OpenShock.Common.Redis.PubSub;

public class CaptiveMessage
{
    public required Guid DeviceId { get; set; }
    public required bool Enabled { get; set; }
}