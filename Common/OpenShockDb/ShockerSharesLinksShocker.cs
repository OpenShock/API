namespace OpenShock.Common.OpenShockDb;

public partial class ShockerSharesLinksShocker : SafetySettings
{
    public Guid ShareLinkId { get; set; }

    public Guid ShockerId { get; set; }

    public int? Cooldown { get; set; }

    public virtual ShockerSharesLink ShareLink { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
