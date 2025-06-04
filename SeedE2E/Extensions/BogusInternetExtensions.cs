using Bogus.DataSets;
using System.Net;

namespace OpenShock.SeedE2E.Extensions;

public static class BogusInternetExtensions
{
    public static IPAddress IpVAnyAddress(this Internet internet, float v6Weight)
    {
        return internet.Random.Bool(v6Weight) ? internet.Ipv6Address() : internet.IpAddress();
    }
}
