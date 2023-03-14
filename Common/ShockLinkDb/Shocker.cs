using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class Shocker
{
    public Guid Id { get; set; }

    public ushort RfId { get; set; }

    public string Name { get; set; } = null!;

    public Guid Device { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual Device DeviceNavigation { get; set; } = null!;

    public virtual ICollection<ShockerControlLog> ShockerControlLogs { get; } = new List<ShockerControlLog>();

    public virtual ICollection<ShockerShareCode> ShockerShareCodes { get; } = new List<ShockerShareCode>();

    public virtual ICollection<ShockerShare> ShockerShares { get; } = new List<ShockerShare>();
}
