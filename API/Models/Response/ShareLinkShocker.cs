using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public class ShareLinkShocker
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }
    public required PauseReason Paused { get; set; }
}