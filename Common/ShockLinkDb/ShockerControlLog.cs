using System;
using System.Collections.Generic;
using ShockLink.Common.Models;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockerControlLog
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid ControlledBy { get; set; }

    public byte Intensity { get; set; }

    public uint Duration { get; set; }
    
    public ControlType Type { get; set; }

    public string? CustomName { get; set; }

    public virtual User ControlledByNavigation { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
