using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class Shocker
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required ShockerModelType Model { get; set; }

    public required ushort RfId { get; set; }

    public required Guid DeviceId { get; set; }

    public bool IsPaused { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public Device Device { get; set; } = null!;
    public ICollection<UserShareInviteShocker> UserShareInviteShockers { get; } = [];
    public ICollection<ShockerControlLog> ShockerControlLogs { get; } = [];
    public ICollection<ShockerShareCode> ShockerShareCodes { get; } = [];
    public ICollection<UserShare> UserShares { get; } = [];
    public ICollection<PublicShareShocker> PublicShareMappings { get; } = [];
}
