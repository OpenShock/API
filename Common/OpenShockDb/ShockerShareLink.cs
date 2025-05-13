namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShareLink
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<ShockerShareLinkShocker> ShockerMappings { get; set; } = new List<ShockerShareLinkShocker>();
}
