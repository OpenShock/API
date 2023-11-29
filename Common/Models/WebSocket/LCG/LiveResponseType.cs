namespace OpenShock.Common.Models.WebSocket.LCG;

public enum LiveResponseType
{
    ServerFrame = 0,
    
    DeviceNotConnected = 100,
    ShockerNotFound = 101,
    
    InvalidData = 200
}