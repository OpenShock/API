using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class ShockerControlLog
{
    public Guid Id { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? ControlledByUserId { get; set; }

    public byte Intensity { get; set; }

    public uint Duration { get; set; }
    
    public ControlType Type { get; set; }

    public string? CustomName { get; set; }

    public bool LiveControl { get; set; }

    public virtual Shocker Shocker { get; set; } = null!;

    public virtual User? ControlledByUser { get; set; }
}
