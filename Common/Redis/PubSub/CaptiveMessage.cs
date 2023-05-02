using ShockLink.Common.Models;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShockLink.Common.Redis.PubSub;

public class CaptiveMessage
{
    public required Guid DeviceId { get; set; }
    public required bool Enabled { get; set; }
}