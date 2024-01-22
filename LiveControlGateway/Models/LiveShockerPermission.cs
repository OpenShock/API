using OpenShock.ServicesCommon.Models;

namespace OpenShock.LiveControlGateway.Models;

public sealed class LiveShockerPermission
{
    public required bool Paused { get; set; }
    public required SharePermsAndLimitsLive PermsAndLimits { get; set; }
}