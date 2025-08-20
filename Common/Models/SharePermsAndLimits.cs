namespace OpenShock.Common.Models;

public class SharePermsAndLimits
{
    public required bool Sound { get; init; }
    public required bool Vibrate { get; init; }
    public required bool Shock { get; init; }
    public required ushort? Duration { get; init; }
    public required byte? Intensity { get; init; }
    public required bool Live { get; init; }
}