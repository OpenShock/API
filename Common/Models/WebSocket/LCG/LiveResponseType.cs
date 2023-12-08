using System.Text.Json.Serialization;

namespace OpenShock.Common.Models.WebSocket.LCG;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveResponseType
{
    Frame = 0,
    
    DeviceNotConnected = 100,
    DeviceConnected = 101,
    ShockerNotFound = 150,
    
    InvalidData = 200,
    RequestTypeNotFound = 201,
    
    Ping = 1000,
    LatencyAnnounce = 1001
}