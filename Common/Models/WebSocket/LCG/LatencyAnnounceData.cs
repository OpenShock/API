namespace OpenShock.Common.Models.WebSocket.LCG;

public sealed class LatencyAnnounceData
{
    public required ushort DeviceLatency { get; set; }
    public required ushort OwnLatency { get; set; }
}