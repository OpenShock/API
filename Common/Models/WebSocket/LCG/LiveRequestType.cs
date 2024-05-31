namespace OpenShock.Common.Models.WebSocket.LCG;

public enum LiveRequestType
{
    Frame = 0,
    BulkFrame = 1,
    
    Pong = 1000
}