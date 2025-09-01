using MessagePack;
using OpenShock.Common.Models;
using Semver;

namespace OpenShock.Common.Redis.PubSub;

[MessagePackObject]
public sealed class DeviceMessage
{
    [Key(0)] public required IDeviceMessagePayload Payload { get; init; }

    public static DeviceMessage Create(DeviceTriggerType type) => new()
    {
        Payload = new DeviceTriggerPayload { Type = type }
    };

    public static DeviceMessage Create(DeviceToggleTarget target, bool state) => new()
    {
        Payload = new DeviceTogglePayload { Target = target, State = state }
    };

    public static DeviceMessage Create(DeviceControlPayload payload) => new()
    {
        Payload = payload
    };

    public static DeviceMessage Create(DeviceOtaInstallPayload payload) => new()
    {
        Payload = payload
    };
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
    [Key(0)] public required DeviceTriggerType Type { get; init; }
}

public enum DeviceToggleTarget : byte
{
    CaptivePortal = 0,
}

[MessagePackObject]
public sealed class DeviceTogglePayload : IDeviceMessagePayload
{
    [Key(0)] public required DeviceToggleTarget Target { get; init; }
    [Key(1)] public required bool State { get; init; }
}

[MessagePackObject]
public sealed class DeviceControlPayload : IDeviceMessagePayload
{
    [Key(0)] public required List<ShockerControlCommand> Controls { get; init; }
}

[MessagePackObject]
public sealed class ShockerControlCommand
{
    [Key(0)] public required Guid ShockerId { get; init; }
    [Key(1)] public required ushort RfId { get; init; }
    [Key(2)] public required byte Intensity { get; init; }
    [Key(3)] public required ushort Duration { get; init; }
    [Key(4)] public required ControlType Type { get; init; }
    [Key(5)] public required ShockerModelType Model { get; init; }
    [Key(6)] public required bool Exclusive { get; init; }
}

[MessagePackObject]
public sealed class DeviceOtaInstallPayload : IDeviceMessagePayload
{
    [Key(0)]
    [MessagePackFormatter(typeof(SemVersionMessagePackFormatter))]
    public required SemVersion Version { get; init; }
}