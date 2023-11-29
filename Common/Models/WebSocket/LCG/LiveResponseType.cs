using System.Text.Json.Serialization;

namespace OpenShock.Common.Models.WebSocket.LCG;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveResponseType
{
    ServerFrame = 0,
    
    DeviceNotConnected = 100,
    ShockerNotFound = 101,
    
    InvalidData = 200,
    RequestTypeNotFound = 201
}