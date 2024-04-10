using System.Text.Json.Serialization;

namespace OpenShock.Common.Models.WebSocket.LCG;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveResponseType
{
    Welcome = 0,
    
    
    Frame = 10,
    
    DeviceNotConnected = 100,
    DeviceConnected = 101,
    ShockerNotFound = 150,
    ShockerMissingLivePermission = 151,
    ShockerMissingPermission = 152,
    ShockerPaused = 153,
    
    InvalidData = 200,
    RequestTypeNotFound = 201,
    
    Ping = 1000,
    LatencyAnnounce = 1001
}