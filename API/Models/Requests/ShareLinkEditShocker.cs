using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class ShareLinkEditShocker
{
    public required bool PermSound { get; set; }

    public required bool PermVibrate { get; set; }

    public required bool PermShock { get; set; }
    
    [Range(300, 30000)]
    public required uint? LimitDuration { get; set; }
    [Range(1, 100)]

    public required byte? LimitIntensity { get; set; }

    [Range(300, 30000)]
    public int? Cooldown { get; set; }
}