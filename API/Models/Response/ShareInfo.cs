using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class ShareInfo
{
    public required BasicUserInfo SharedWith { get; init; }
    public required DateTime CreatedOn { get; init; }
    public required ShockerPermissions Permissions { get; init; }
    public required ShockerLimits Limits { get; init; }
    public required bool Paused { get; init; }
}