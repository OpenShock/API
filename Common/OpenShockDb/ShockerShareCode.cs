namespace OpenShock.Common.OpenShockDb;

public sealed class ShockerShareCode : SafetySettings
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Shocker Shocker { get; set; } = null!;
}
