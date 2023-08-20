using ShockLink.API.Models.Response;

namespace ShockLink.API.Models.Requests;

public class CreateShareCode
{
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }
}