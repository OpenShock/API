using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class Shocker
{
    public Guid Id { get; set; }

    public ushort RfId { get; set; }

    public string Name { get; set; } = null!;

    public Guid DeviceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsPaused { get; set; }
    
    public ShockerModelType Model { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<ShareRequestShocker> ShareRequestMappings { get; set; } = new List<ShareRequestShocker>();

    public virtual ICollection<ShockerControlLog> ShockerControlLogs { get; set; } = new List<ShockerControlLog>();

    public virtual ICollection<ShockerShareCode> ShockerShareCodes { get; set; } = new List<ShockerShareCode>();

    public virtual ICollection<ShockerShare> ShockerShares { get; set; } = new List<ShockerShare>();

    public virtual ICollection<ShockerShareLinkShocker> ShareLinkMappings { get; set; } = new List<ShockerShareLinkShocker>();
}
