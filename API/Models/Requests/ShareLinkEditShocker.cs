using System.ComponentModel.DataAnnotations;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Models.Requests;

public sealed class ShareLinkEditShocker
{
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }

    [Range(300, 30000)]
    public int? Cooldown { get; set; }
}