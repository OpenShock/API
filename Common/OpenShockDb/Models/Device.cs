namespace OpenShock.Common.OpenShockDb;

public sealed class Device
{
    public required Guid Id { get; init; }

    public required Guid OwnerId { get; init; }

    public required string Name { get; set; }

    public required string Token { get; set; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User Owner { get; init; } = null!;
    public ICollection<Shocker> Shockers { get; } = [];
    public ICollection<DeviceOtaUpdate> OtaUpdates { get; } = [];
}
