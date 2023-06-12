using ShockLink.Common.Models;

namespace ShockLink.API.DeviceControl;

public class ControlShockerObj
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required ushort RfId { get; set; }
    public required Guid Device { get; set; }
    public required Guid Owner { get; set; }
    public required ShockerModelType Model { get; set; }
    public required bool Paused { get; set; }
    public required SharePermsAndLimits? PermsAndLimits { get; set; }
    
    public class SharePermsAndLimits
    {
        public required bool Sound { get; set; }
        public required bool Vibrate { get; set; }
        public required bool Shock { get; set; }
        public required uint? Duration { get; set; }
        public required byte? Intensity { get; set; }
    }
    
}