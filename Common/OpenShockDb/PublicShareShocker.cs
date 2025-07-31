namespace OpenShock.Common.OpenShockDb;

public sealed class PublicShareShocker : SafetySettings
{
    public required Guid PublicShareId { get; set; }

    public required Guid ShockerId { get; set; }

    public int? Cooldown { get; set; } // TODO: Should probably be UInt

    // Navigations
    public PublicShare PublicShare { get; set; } = null!;
    public Shocker Shocker { get; set; } = null!;
}
