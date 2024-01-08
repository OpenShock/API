using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShare
{
    public Guid ShockerId { get; set; }

    public Guid SharedWith { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShock { get; set; }

    public ushort? LimitDuration { get; set; }

    public byte? LimitIntensity { get; set; }

    public bool Paused { get; set; }

    public bool PermLive { get; set; }

    public virtual User SharedWithNavigation { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
