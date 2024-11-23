using OpenShock.Common.Utils;

namespace OpenShock.Common.Config;

public sealed class MetricsConfig
{
    public IReadOnlyCollection<string> AllowedNetworks { get; init; } = TrustedProxiesFetcher.PrivateNetworks;
}