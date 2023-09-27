namespace OpenShock.Common.Models;

public class ControlRequest
{
    public required IEnumerable<WebSocket.User.Control> Shocks { get; set; }
    public required string? CustomName { get; set; }
}