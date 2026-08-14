using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class TokenResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required DateTime CreatedOn { get; init; }
    
    public required DateTime? ValidUntil { get; init; }
    
    public required DateTime LastUsed { get; init; }
    
    public required List<PermissionType> Permissions { get; init; }

    public static TokenResponse MapFrom(TokenResponseV2 token) => new()
    {
        Id = token.Id,
        Name = token.Name,
        CreatedOn = token.CreatedOn,
        ValidUntil = token.ValidUntil,
        LastUsed = token.LastUsed ?? DateTime.MinValue,
        Permissions = [.. token.Permissions]
    };
}