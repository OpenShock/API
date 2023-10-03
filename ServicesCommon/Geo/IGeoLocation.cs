using System.Net;
using OpenShock.Common.Redis;

namespace OpenShock.ServicesCommon.Geo;

public interface IGeoLocation
{
    public Task<LcgNode?> GetClosestNode(IPAddress ipAddress);
}