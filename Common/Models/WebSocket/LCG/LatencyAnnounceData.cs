namespace OpenShock.Common.Models.WebSocket.LCG;

public sealed class LatencyAnnounceData
{
    public required ulong DeviceLatency { get; set; }
    public required ulong OwnLatency { get; set; }
}