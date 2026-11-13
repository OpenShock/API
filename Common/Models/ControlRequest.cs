using OpenShock.Common.Models.WebSocket.User;

namespace OpenShock.Common.Models;

public sealed class ControlRequest
{
    public required IReadOnlyList<Control> Shocks { get; set; }
    public string? CustomName { get; set; } = null;
}