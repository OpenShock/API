namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequestsShocker : SafetySettings
{
    public Guid ShareRequest { get; set; }

    public Guid Shocker { get; set; }

    public virtual ShareRequest ShareRequestNavigation { get; set; } = null!;

    public virtual Shocker ShockerNavigation { get; set; } = null!;
}
