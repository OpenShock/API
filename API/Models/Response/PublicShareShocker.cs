using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class PublicShareShocker
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required ShockerPermissions Permissions { get; init; }
    public required ShockerLimits Limits { get; init; }
    public required PauseReason Paused { get; init; }
}