using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class ShareInfo
{
    public required GenericIni SharedWith { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required PermissionsObj Permissions { get; set; }
    public required LimitObj Limits { get; set; }

    public class PermissionsObj
    {
        public required bool Vibrate { get; set; }
        public required bool Sound { get; set; }
        public required bool Shock { get; set; }
    }
    
    public class LimitObj
    {
        public required byte? Intensity { get; set; }
        public required uint? Duration { get; set; }
    }
}