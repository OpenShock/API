using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class ShockerShareCode
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool? PermSound { get; set; }

    public bool? PermVibrate { get; set; }

    public bool? PermShock { get; set; }

    public ushort? LimitDuration { get; set; }

    public byte? LimitIntensity { get; set; }

    public virtual Shocker Shocker { get; set; } = null!;
}
