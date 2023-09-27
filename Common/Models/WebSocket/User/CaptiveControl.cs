namespace OpenShock.Common.Models.WebSocket.User;

public class CaptiveControl
{
    public required Guid DeviceId { get; set; }
    public required bool Enabled { get; set; }
}