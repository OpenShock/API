using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockerShareCode
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool PermSound { get; set; }

    public bool PermVibrate { get; set; }

    public bool PermShock { get; set; }

    public uint? LimitDuration { get; set; }

    public byte? LimitIntensity { get; set; }
}
