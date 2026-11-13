namespace OpenShock.Common.OpenShockDb;

public sealed class DeviceOtaUpdate
{
    public required Guid DeviceId { get; init; }

    public required int UpdateId { get; init; }

    public required OtaUpdateStatus Status { get; set; }

    public required string Version { get; init; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public Device Device { get; init; } = null!;
}
