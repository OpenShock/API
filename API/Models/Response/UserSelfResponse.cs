using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class UserSelfResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required Uri Image { get; init; }
    public required List<RoleType> Roles { get; init; }
    public required string Rank { get; init; }
    public required bool HasPassword { get; init; }
}
