using System.ComponentModel.DataAnnotations;
using OpenShock.API.Models.Response;
using OpenShock.Common;

namespace OpenShock.API.Models.Requests;

public sealed class ShareLinkEditShocker
{
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }

    [Range(Constants.MinControlDuration, Constants.MaxControlDuration)]
    public ushort? Cooldown { get; set; }
}