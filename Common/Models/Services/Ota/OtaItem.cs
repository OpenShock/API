using Semver;

namespace OpenShock.Common.Models.Services.Ota;

public sealed class OtaItem
{
    public required DateTimeOffset StartedAt { get; init; }
    public required OtaUpdateStatus Status { get; init; }
    public required SemVersion Version { get; init; }
}