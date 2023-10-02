using System.Net;

namespace OpenShock.ServicesCommon.Geo;

public interface IGeoLocation
{
    public Task GetClosestNode(IPAddress ipAddress);
}