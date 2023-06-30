using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockerSharesLinksShocker
{
    public Guid ShareLinkId { get; set; }

    public Guid ShockerId { get; set; }

    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShock { get; set; }

    public int? LimitDuration { get; set; }

    public short? LimitIntensity { get; set; }

    public int? Cooldown { get; set; }

    public virtual ShockerSharesLink ShareLink { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
