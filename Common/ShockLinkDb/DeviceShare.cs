using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class DeviceShare
{
    public Guid DeviceId { get; set; }

    public Guid SharedWith { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual User SharedWithNavigation { get; set; } = null!;
}
