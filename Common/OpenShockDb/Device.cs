namespace OpenShock.Common.OpenShockDb;

public partial class Device
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Token { get; set; } = null!;

    public virtual ICollection<DeviceOtaUpdate> OtaUpdates { get; set; } = new List<DeviceOtaUpdate>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Shocker> Shockers { get; set; } = new List<Shocker>();
}
