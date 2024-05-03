using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class TokenResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; } = null!;

    public required DateTime CreatedOn { get; set; }

    public required string CreatedByIp { get; set; } = null!;

    public required DateTime? ValidUntil { get; set; }
    
    public required List<PermissionType> Permissions { get; set; }
}