namespace OpenShock.Common.OpenShockDb;

public sealed class PublicShareShocker : SafetySettings
{
    public required Guid PublicShareId { get; init; }

    public required Guid ShockerId { get; init; }

    public int? Cooldown { get; set; } // TODO: Should probably be UInt

    // Navigations
    public PublicShare PublicShare { get; init; } = null!;
    public Shocker Shocker { get; init; } = null!;
}
