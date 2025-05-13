namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequest
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? UserId { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<ShareRequestsShocker> ShockerMappings { get; set; } = new List<ShareRequestsShocker>();

    public virtual User? User { get; set; }
}
