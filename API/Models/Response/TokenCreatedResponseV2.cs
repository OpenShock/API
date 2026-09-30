using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class TokenCreatedResponseV2
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Token { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime? ValidUntil { get; init; }

    public required DateTime? LastUsed { get; init; }

    public required IReadOnlyList<PermissionType> Permissions { get; init; }

    public required ShockerControlSettings ShockerControl { get; init; }
}
