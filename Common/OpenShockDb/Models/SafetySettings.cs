namespace OpenShock.Common.OpenShockDb;

public class SafetySettings
{
    public required bool AllowShock { get; set; }

    public required bool AllowVibrate { get; set; }

    public required bool AllowSound { get; set; }

    public required bool AllowLiveControl { get; set; }

    public byte? MaxIntensity { get; set; }

    public ushort? MaxDuration { get; set; }

    public required bool IsPaused { get; set; }
}