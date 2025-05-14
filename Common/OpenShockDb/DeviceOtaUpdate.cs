using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class DeviceOtaUpdate
{
    public Guid DeviceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Version { get; set; } = null!;

    public int UpdateId { get; set; }
    
    public OtaUpdateStatus Status { get; set; }

    public string? Message { get; set; }

    public virtual Device Device { get; set; } = null!;
}
