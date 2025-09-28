namespace OpenShock.Common.Services;

public interface IWebSocketMeter
{
    void RegisterMessageSize(int sizeBytes);
}