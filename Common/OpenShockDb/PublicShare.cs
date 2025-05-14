namespace OpenShock.Common.OpenShockDb;

public partial class PublicShare
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<PublicShareShocker> ShockerMappings { get; set; } = new List<PublicShareShocker>();
}
