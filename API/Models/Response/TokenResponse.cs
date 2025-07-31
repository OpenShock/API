using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class TokenResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required DateTime CreatedOn { get; init; }
    
    public required DateTime? ValidUntil { get; init; }
    
    public required DateTime LastUsed { get; init; }
    
    public required List<PermissionType> Permissions { get; init; }
}