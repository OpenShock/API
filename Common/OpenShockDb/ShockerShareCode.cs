namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShareCode : SafetySettings
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shocker Shocker { get; set; } = null!;
}
