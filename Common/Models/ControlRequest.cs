using OpenShock.Common.Models.WebSocket.User;

namespace OpenShock.Common.Models;

public sealed class ControlRequest
{
    public required IReadOnlyList<Control> Shocks { get; init; }
    public string? CustomName { get; init; } = null;
}