namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShare : SafetySettings
{
    public Guid ShockerId { get; set; }

    public Guid SharedWithUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User SharedWithUser { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
