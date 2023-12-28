namespace OpenShock.ServicesCommon.Models;

public class SharePermsAndLimits
{
    public required bool Sound { get; set; }
    public required bool Vibrate { get; set; }
    public required bool Shock { get; set; }
    public required ushort? Duration { get; set; }
    public required byte? Intensity { get; set; }
}

public class SharePermsAndLimitsLive : SharePermsAndLimits
{
    public required bool Live { get; set; }
}