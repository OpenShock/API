using System;
using System.Collections.Generic;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class DeviceOtaUpdate
{
    public Guid Device { get; set; }

    public DateTime CreatedOn { get; set; }

    public string Version { get; set; } = null!;

    public int UpdateId { get; set; }
    
    public OtaUpdateStatus Status { get; set; }

    public string? Message { get; set; }

    public virtual Device DeviceNavigation { get; set; } = null!;
}
