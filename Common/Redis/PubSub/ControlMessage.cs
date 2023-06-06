using ShockLink.Common.Models;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShockLink.Common.Redis.PubSub;

public class ControlMessage
{
    public required Guid Shocker { get; set; }
    public required IDictionary<Guid, IList<ShockerControlInfo>> ControlMessages { get; set; }

    public class ShockerControlInfo
    {
        public required Guid Id { get; set; }
        public required ushort RfId { get; set; }
        public required byte Intensity { get; set; }
        public required uint Duration { get; set; }
        public required ControlType Type { get; set; }
        public required ShockerModel Model { get; set; }
    }
}