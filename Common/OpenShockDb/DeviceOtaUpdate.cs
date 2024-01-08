using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class DeviceOtaUpdate
{
    public Guid Device { get; set; }

    public Guid UpdateId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string Version { get; set; } = null!;

    public virtual Device DeviceNavigation { get; set; } = null!;
}
