namespace OpenShock.Common.OpenShockDb;

public partial class ShockerSharesLink
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<ShockerSharesLinksShocker> ShockerSharesLinksShockers { get; set; } = new List<ShockerSharesLinksShocker>();
}
