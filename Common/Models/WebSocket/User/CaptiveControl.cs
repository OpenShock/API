namespace OpenShock.Common.Models.WebSocket.User;

public sealed class CaptiveControl
{
    public required Guid DeviceId { get; set; }
    public required bool Enabled { get; set; }
}