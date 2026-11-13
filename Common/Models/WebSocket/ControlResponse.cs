using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Models.WebSocket;

public sealed class ControlResponse
{
    public required ushort Id { get; init; }
    public required ControlType Type { get; init; }
    public required byte Intensity { get; init; }
    public required uint Duration { get; init; }
    public required ShockerModelType Model { get; init; }
}
