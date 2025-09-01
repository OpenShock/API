using MessagePack;

namespace OpenShock.Common.Redis.PubSub;

[MessagePackObject]
public sealed class DeviceStatus
{
    [Key(0)] public DeviceStatusType Type { get; init; }
    [Key(1)] public required IDeviceStatusPayload Payload { get; init; }

    public static DeviceStatus Create(DeviceBoolStateType stateType, bool state) => new()
    {
        Type = DeviceStatusType.BoolStateChanged,
        Payload = new DeviceBoolStatePayload
        {
            Type = stateType,
            State = state
        }
    };
}

public enum DeviceStatusType : byte
{
    BoolStateChanged = 0,
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
    [Key(0)] public DeviceBoolStateType Type { get; init; }
    [Key(1)] public bool State { get; init; }
}