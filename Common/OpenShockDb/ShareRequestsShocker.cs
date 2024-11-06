using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequestsShocker
{
    public Guid ShareRequest { get; set; }

    public Guid Shocker { get; set; }

    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShock { get; set; }

    public ushort? LimitDuration { get; set; }

    public byte? LimitIntensity { get; set; }

    public bool PermLive { get; set; }

    public virtual ShareRequest ShareRequestNavigation { get; set; } = null!;

    public virtual Shocker ShockerNavigation { get; set; } = null!;
}
