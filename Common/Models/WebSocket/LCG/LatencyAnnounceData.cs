namespace OpenShock.Common.Models.WebSocket.LCG;

public sealed class LatencyAnnounceData
{
    public required ushort DeviceLatency { get; init; }
    public required ushort OwnLatency { get; init; }
}