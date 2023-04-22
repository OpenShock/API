using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class ApiTokenResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string CreatedByIp { get; set; } = null!;

    public DateTime? ValidUntil { get; set; }
    
    public List<PermissionType> Permissions { get; set; }
}