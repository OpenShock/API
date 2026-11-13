namespace OpenShock.Common.Models.WebSocket.User;

public sealed class CaptiveControl
{
    public required Guid DeviceId { get; init; }
    public required bool Enabled { get; init; }
}