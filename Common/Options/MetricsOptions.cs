using OpenShock.Common.Utils;

namespace OpenShock.Common.Options;

public sealed class MetricsOptions
{
    public IReadOnlyCollection<string> AllowedNetworks { get; init; } = TrustedProxiesFetcher.PrivateNetworks;
}