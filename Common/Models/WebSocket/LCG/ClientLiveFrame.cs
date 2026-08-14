using System.Text.Json.Serialization;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Models.WebSocket.LCG;

public sealed class ClientLiveFrame
{
    public required Guid Shocker { get; set; }
    public required byte Intensity { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ControlType Type { get; set; }
}