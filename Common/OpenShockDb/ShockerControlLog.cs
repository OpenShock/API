using System;
using System.Collections.Generic;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class ShockerControlLog
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid? ControlledBy { get; set; }

    public byte Intensity { get; set; }

    public uint Duration { get; set; }
    
    public ControlType Type { get; set; }

    public string? CustomName { get; set; }

    public bool LiveControl { get; set; }

    public virtual User? ControlledByNavigation { get; set; }

    public virtual Shocker Shocker { get; set; } = null!;
}
