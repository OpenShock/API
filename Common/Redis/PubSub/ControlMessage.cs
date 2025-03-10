using OpenShock.Common.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace OpenShock.Common.Redis.PubSub;

public sealed class ControlMessage
{
    public required Guid Sender { get; set; }
    
    /// <summary>
    /// Guid is the device id
    /// </summary>
    public required IDictionary<Guid, IReadOnlyList<ShockerControlInfo>> ControlMessages { get; set; }

    public sealed class ShockerControlInfo
    {
        public required Guid Id { get; set; }
        public required ushort RfId { get; set; }
        public required byte Intensity { get; set; }
        public required ushort Duration { get; set; }
        public required ControlType Type { get; set; }
        public required ShockerModelType Model { get; set; }
        public bool Exclusive { get; set; } = false;
    }
}