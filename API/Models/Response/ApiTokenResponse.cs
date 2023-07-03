using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class ApiTokenResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; } = null!;

    public required DateTime CreatedOn { get; set; }

    public required string CreatedByIp { get; set; } = null!;

    public required DateTime? ValidUntil { get; set; }
    
    public required List<PermissionType> Permissions { get; set; }
}