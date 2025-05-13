namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShare : SafetySettings
{
    public Guid ShockerId { get; set; }

    public Guid SharedWith { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User SharedWithNavigation { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
