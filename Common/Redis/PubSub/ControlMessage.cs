using ShockLink.Common.Models;

namespace ShockLink.Common.Redis.PubSub;

public class ControlMessage
{
    public required Guid Shocker { get; set; }
    public required IEnumerable<DeviceControlInfo> ControlMessages { get; set; }

    public class DeviceControlInfo
    {
        public required Guid DeviceId { get; set; }
        public required IEnumerable<ShockerControlInfo> Shocks { get; set; }
        
        public class ShockerControlInfo
        {
            public required Guid Id { get; set; }
            public required ushort RfId { get; set; }
            public required byte Intensity { get; set; }
            public required uint Duration { get; set; }
            public required ControlType Type { get; set; }
        }
    }
    

}