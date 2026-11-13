namespace OpenShock.Common.OpenShockDb;

public sealed class ShockerShareCode : SafetySettings
{
    public Guid Id { get; init; }

    public Guid ShockerId { get; init; }

    public DateTime CreatedAt { get; init; }

    public Shocker Shocker { get; init; } = null!;
}
