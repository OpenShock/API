using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public class ShareInfo
{
    public required GenericIni SharedWith { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }
    public required bool Paused { get; set; }
}