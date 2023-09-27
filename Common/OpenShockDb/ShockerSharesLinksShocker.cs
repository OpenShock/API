namespace OpenShock.Common.OpenShockDb;

public partial class ShockerSharesLinksShocker
{
    public Guid ShareLinkId { get; set; }

    public Guid ShockerId { get; set; }

    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShock { get; set; }

    public uint? LimitDuration { get; set; }

    public byte? LimitIntensity { get; set; }

    public int? Cooldown { get; set; }

    public bool Paused { get; set; }

    public virtual ShockerSharesLink ShareLink { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
