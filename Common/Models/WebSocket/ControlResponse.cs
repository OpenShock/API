namespace OpenShock.Common.Models.WebSocket;

public class ControlResponse
{
    public required ushort Id { get; set; }
    public required ControlType Type { get; set; }
    public required byte Intensity { get; set; }
    public required uint Duration { get; set; }
    public required ShockerModelType Model { get; set; }
}