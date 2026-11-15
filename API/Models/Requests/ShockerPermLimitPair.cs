using OpenShock.API.Models.Response;

namespace OpenShock.API.Models.Requests;

public class ShockerPermLimitPair
{
    public required ShockerPermissions Permissions { get; init; }
    public required ShockerLimits Limits { get; init; }
}
