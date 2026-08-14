namespace OpenShock.Common.OpenShockDb;

public sealed class Device
{
    public required Guid Id { get; set; }

    public required Guid OwnerId { get; set; }

    public required string Name { get; set; }

    public required string Token { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User Owner { get; set; } = null!;
    public ICollection<Shocker> Shockers { get; } = [];
    public ICollection<DeviceOtaUpdate> OtaUpdates { get; } = [];
}
