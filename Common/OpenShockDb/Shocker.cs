using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class Shocker
{
    public Guid Id { get; set; }

    public ushort RfId { get; set; }

    public string Name { get; set; } = null!;

    public Guid Device { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Paused { get; set; }
    
    public ShockerModelType Model { get; set; }

    public virtual Device DeviceNavigation { get; set; } = null!;

    public virtual ICollection<ShareRequestsShocker> ShareRequestsShockers { get; set; } = new List<ShareRequestsShocker>();

    public virtual ICollection<ShockerControlLog> ShockerControlLogs { get; set; } = new List<ShockerControlLog>();

    public virtual ICollection<ShockerShareCode> ShockerShareCodes { get; set; } = new List<ShockerShareCode>();

    public virtual ICollection<ShockerShare> ShockerShares { get; set; } = new List<ShockerShare>();

    public virtual ICollection<ShockerSharesLinksShocker> ShockerSharesLinksShockers { get; set; } = new List<ShockerSharesLinksShocker>();
}
