using ShockLink.API.Models.Response;

namespace ShockLink.API.Models.Requests;

public class CreateShareCode
{
    public required ShareInfo.PermissionsObj Permissions { get; set; }
    public required ShareInfo.LimitObj Limits { get; set; }
}