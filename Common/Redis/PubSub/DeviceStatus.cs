using MessagePack;

namespace OpenShock.Common.Redis.PubSub;

[MessagePackObject]
public sealed class DeviceStatus
{
    [Key(0)] public required Guid DeviceId { get; init; }
    [Key(1)] public required IDeviceStatusPayload Payload { get; init; }

    public static DeviceStatus Create(Guid deviceId, DeviceBoolStateType stateType, bool state) => new()
    {
        DeviceId = deviceId,
        Payload = new DeviceBoolStatePayload
        {
            Type = stateType,
            State = state
        }
    };
}

[Union(0, typeof(DeviceBoolStatePayload))]
public interface IDeviceStatusPayload;

public enum DeviceBoolStateType : byte
{
    Online = 0,
    EStopped = 1
}

[MessagePackObject]
public sealed class DeviceBoolStatePayload : IDeviceStatusPayload
{
    [Key(0)] public required DeviceBoolStateType Type { get; init; }
    [Key(1)] public required bool State { get; init; }
}