namespace OpenShock.Common.Models;

public sealed class ControlRequest
{
    public required IReadOnlyList<WebSocket.User.Control> Shocks { get; set; }
    public string? CustomName { get; set; } = null;
}