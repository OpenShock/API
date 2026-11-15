using System.Net;
using Bogus.DataSets;

namespace OpenShock.SeedE2E.Extensions;

public static class BogusInternetExtensions
{
    public static IPAddress IpVAnyAddress(this Internet internet, float v6Weight)
    {
        return internet.Random.Bool(v6Weight) ? internet.Ipv6Address() : internet.IpAddress();
    }
}
