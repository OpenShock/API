namespace OpenShock.Common.Options;

public sealed class MetricsOptions
{
    public required IReadOnlyCollection<string> AllowedNetworks { get; init; }
}