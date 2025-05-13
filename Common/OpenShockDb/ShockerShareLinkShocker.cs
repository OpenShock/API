namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShareLinkShocker : SafetySettings
{
    public Guid ShareLinkId { get; set; }

    public Guid ShockerId { get; set; }

    public int? Cooldown { get; set; }

    public virtual ShockerShareLink ShareLink { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
