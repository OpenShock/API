using System.ComponentModel.DataAnnotations;
using ShockLink.API.Models.Response;

namespace ShockLink.API.Models.Requests;

public class ShareLinkEditShocker
{
    public required ShockerPermissions Permissions { get; set; }
    public required ShockerLimits Limits { get; set; }

    [Range(300, 30000)]
    public int? Cooldown { get; set; }
}