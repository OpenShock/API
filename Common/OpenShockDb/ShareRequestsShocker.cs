namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequestsShocker : SafetySettings
{
    public Guid ShareRequestId { get; set; }

    public Guid ShockerId { get; set; }

    public virtual ShareRequest ShareRequest { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
