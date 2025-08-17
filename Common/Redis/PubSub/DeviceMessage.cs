using MessagePack;
using OpenShock.Common.Models;
using Semver;

namespace OpenShock.Common.Redis.PubSub;

[MessagePackObject]
public sealed class DeviceMessage
{
    [Key(0)] public DeviceMessageType Type { get; init; }

    [Key(1)] public required IDeviceMessagePayload Payload { get; init; }

    public static DeviceMessage Create(DeviceTriggerType type) => new()
    {
        Type = DeviceMessageType.Trigger,
        Payload = new DeviceTriggerPayload { Type = type }
    };

    public static DeviceMessage Create(DeviceToggleTarget target, bool state) => new()
    {
        Type = DeviceMessageType.Toggle,
        Payload = new DeviceTogglePayload { Target = target, State = state }
    };

    public static DeviceMessage Create(DeviceControlPayload payload) => new()
    {
        Type = DeviceMessageType.Control,
        Payload = payload
    };

    public static DeviceMessage Create(DeviceOtaInstallPayload payload) => new()
    {
        Type = DeviceMessageType.OtaInstall,
        Payload = payload
    };
}

public enum DeviceMessageType : byte
{
    Trigger = 0,
    Toggle = 1,
    Control = 2,
    OtaInstall = 3
}

[Union(0, typeof(DeviceTriggerPayload))]
[Union(1, typeof(DeviceTogglePayload))]
[Union(2, typeof(DeviceControlPayload))]
[Union(3, typeof(DeviceOtaInstallPayload))]
public interface IDeviceMessagePayload;

public enum DeviceTriggerType : byte
{
    DeviceInfoUpdated = 0,
    DeviceEmergencyStop = 1,
    DeviceReboot = 2
}

[MessagePackObject]
public sealed class DeviceTriggerPayload : IDeviceMessagePayload
{
    [Key(0)] public DeviceTriggerType Type { get; init; }
}

public enum DeviceToggleTarget : byte
{
    CaptivePortal = 0,
}

[MessagePackObject]
public sealed class DeviceTogglePayload : IDeviceMessagePayload
{
    [Key(0)] public DeviceToggleTarget Target { get; init; }
    [Key(1)] public bool State { get; init; }
}

[MessagePackObject]
public sealed class DeviceControlPayload : IDeviceMessagePayload
{
    [Key(0)] public required ShockerControlInfo[] Controls { get; init; }

    [MessagePackObject]
    public sealed class ShockerControlInfo
    {
        [Key(0)] public ushort RfId { get; init; }
        [Key(1)] public byte Intensity { get; init; }
        [Key(2)] public ushort Duration { get; init; }
        [Key(3)] public ControlType Type { get; init; }
        [Key(4)] public ShockerModelType Model { get; init; }
        [Key(5)] public bool Exclusive { get; init; }
    }
}

[MessagePackObject]
public sealed class DeviceOtaInstallPayload : IDeviceMessagePayload
{
    [Key(0)]
    [MessagePackFormatter(typeof(SemVersionMessagePackFormatter))]
    public required SemVersion Version { get; init; }
}