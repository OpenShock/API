using MessagePack;

namespace OpenShock.Common.Redis.PubSub;

[MessagePackObject]
public sealed class DeviceStatus
{
    [Key(0)] public Guid DeviceId { get; init; }

    [Key(1)] public DeviceStatusType Type { get; init; }

    public static DeviceStatus Create(Guid deviceId, DeviceStatusType type) => new()
    {
        DeviceId = deviceId,
        Type = DeviceStatusType.Online,
    };
}

public enum DeviceStatusType : byte
{
    Online = 0,
}