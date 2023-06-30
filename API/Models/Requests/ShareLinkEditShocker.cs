namespace ShockLink.API.Models.Requests;

public class ShareLinkEditShocker
{
    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShocker { get; set; }

    public int? LimitDuration { get; set; }

    public short? LimitIntensity { get; set; }

    public int? Cooldown { get; set; }
}