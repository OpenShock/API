using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class DeviceOtaUpdate
{
    public required Guid DeviceId { get; set; }

    public required int UpdateId { get; set; }

    public required OtaUpdateStatus Status { get; set; }

    public required string Version { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public Device Device { get; set; } = null!;
}
