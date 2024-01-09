using System;
using System.Collections.Generic;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class DeviceOtaUpdate
{
    public Guid Device { get; set; }

    public Guid UpdateId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string Version { get; set; } = null!;
    
    public OtaUpdateStatus Status { get; set; }

    public virtual Device DeviceNavigation { get; set; } = null!;
}
