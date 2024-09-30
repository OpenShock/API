using System.Text.Json.Serialization;

namespace OpenShock.Common.Models.WebSocket.LCG;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveResponseType
{
    Frame = 0,

    // TPS for the client to send
    // ReSharper disable once InconsistentNaming
    TPS = 50,

    DeviceNotConnected = 100,
    DeviceConnected = 101,
    ShockerNotFound = 150,
    ShockerMissingLivePermission = 151,
    ShockerMissingPermission = 152,
    ShockerPaused = 153,
    ShockerExclusive = 154,

    InvalidData = 200,
    RequestTypeNotFound = 201,

    Ping = 1000,
    LatencyAnnounce = 1001
}