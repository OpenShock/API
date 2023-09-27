namespace OpenShock.Common.ShockLinkDb;

public partial class ShockerSharesLink
{
    public Guid Id { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? ExpiresOn { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<ShockerSharesLinksShocker> ShockerSharesLinksShockers { get; set; } = new List<ShockerSharesLinksShocker>();
}
