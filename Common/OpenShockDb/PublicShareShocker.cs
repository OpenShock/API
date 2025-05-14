namespace OpenShock.Common.OpenShockDb;

public partial class PublicShareShocker : SafetySettings
{
    public Guid PublicShareId { get; set; }

    public Guid ShockerId { get; set; }

    public int? Cooldown { get; set; }

    public virtual PublicShare PublicShare { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
