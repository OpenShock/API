namespace OpenShock.Common.OpenShockDb;

public sealed class ShareRequestShocker : SafetySettings
{
    public required Guid ShareRequestId { get; set; }

    public required Guid ShockerId { get; set; }

    // Navigations
    public ShareRequest ShareRequest { get; set; } = null!;
    public Shocker Shocker { get; set; } = null!;
}
