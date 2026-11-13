namespace OpenShock.Common.OpenShockDb;

public sealed class Shocker
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required ShockerModelType Model { get; set; }

    public required ushort RfId { get; set; }

    public required Guid DeviceId { get; set; }

    public bool IsPaused { get; set; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public Device Device { get; init; } = null!;
    public ICollection<UserShareInviteShocker> UserShareInviteShockerMappings { get; } = [];
    public ICollection<ShockerControlLog> ShockerControlLogs { get; } = [];
    public ICollection<ControlStat> ControlStats { get; } = [];
    public ICollection<ShockerShareCode> ShockerShareCodes { get; } = [];
    public ICollection<UserShare> UserShares { get; } = [];
    public ICollection<PublicShareShocker> PublicShareMappings { get; } = [];
}
