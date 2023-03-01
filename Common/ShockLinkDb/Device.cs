using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class Device
{
    public Guid Id { get; set; }

    public int RfId { get; set; }

    public Guid Owner { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DeviceShare> DeviceShares { get; } = new List<DeviceShare>();

    public virtual User OwnerNavigation { get; set; } = null!;
}
