namespace OpenShock.Common.Models.WebSocket.LCG;

public sealed class LcgLiveControlPing
{
    public required long Timestamp { get; set; } // Was used for latency calculation, latency calculation is now done serverside
}