using MessagePack;
using MessagePack.Resolvers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;

namespace OpenShock.Common.Tests.Redis;

/// <summary>
/// The API publishes these messages and the gateway consumes them, so during a rolling deploy old and new builds
/// talk to each other. The pinned bytes guard the wire format, the round trips guard the union dispatch.
/// </summary>
public class PubSubSerializationTests
{
    // Same resolver chain as OpenShockApplication sets as the default
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(
        CompositeResolver.Create(NativeGuidResolver.Instance, StandardResolver.Instance));

    private static T RoundTrip<T>(T value) =>
        MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, Options), Options);

    [Test]
    public async Task DeviceMessage_WireFormat_IsUnchanged()
    {
        var bytes = MessagePackSerializer.Serialize(DeviceMessage.Create(DeviceTriggerType.DeviceReboot), Options);

        // [ [unionKey 0, [Type 2]] ]
        await Assert.That(Convert.ToHexString(bytes)).IsEqualTo("9192009102");
    }

    [Test]
    public async Task DeviceStatus_WireFormat_IsUnchanged()
    {
        var deviceId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var bytes = MessagePackSerializer.Serialize(DeviceStatus.Create(deviceId, DeviceBoolStateType.Online, true), Options);
        var guidHex = Convert.ToHexString(deviceId.ToByteArray());

        // [ bin8(16) deviceId, [unionKey 0, [Type 0, State true]] ]
        await Assert.That(Convert.ToHexString(bytes)).IsEqualTo($"92C410{guidHex}92009200C3");
    }

    [Test]
    public async Task DeviceMessage_Trigger_RoundTrips()
    {
        var payload = RoundTrip(DeviceMessage.Create(DeviceTriggerType.DeviceEmergencyStop)).Payload;

        await Assert.That(payload).IsTypeOf<DeviceTriggerPayload>();
        await Assert.That(((DeviceTriggerPayload)payload).Type).IsEqualTo(DeviceTriggerType.DeviceEmergencyStop);
    }

    [Test]
    public async Task DeviceMessage_Toggle_RoundTrips()
    {
        var payload = RoundTrip(DeviceMessage.Create(DeviceToggleTarget.CaptivePortal, true)).Payload;

        await Assert.That(payload).IsTypeOf<DeviceTogglePayload>();
        var toggle = (DeviceTogglePayload)payload;
        await Assert.That(toggle.Target).IsEqualTo(DeviceToggleTarget.CaptivePortal);
        await Assert.That(toggle.State).IsTrue();
    }

    [Test]
    public async Task DeviceMessage_Control_RoundTrips()
    {
        var command = new ShockerControlCommand
        {
            ShockerId = Guid.CreateVersion7(),
            RfId = 1234,
            Intensity = 50,
            Duration = 1000,
            Type = ControlType.Vibrate,
            Model = ShockerModelType.CaiXianlin,
            Exclusive = true
        };

        var payload = RoundTrip(DeviceMessage.Create(new DeviceControlPayload { Controls = [command] })).Payload;

        await Assert.That(payload).IsTypeOf<DeviceControlPayload>();
        var controls = ((DeviceControlPayload)payload).Controls;
        await Assert.That(controls).Count().IsEqualTo(1);
        await Assert.That(controls[0].ShockerId).IsEqualTo(command.ShockerId);
        await Assert.That(controls[0].RfId).IsEqualTo(command.RfId);
        await Assert.That(controls[0].Intensity).IsEqualTo(command.Intensity);
        await Assert.That(controls[0].Duration).IsEqualTo(command.Duration);
        await Assert.That(controls[0].Type).IsEqualTo(command.Type);
        await Assert.That(controls[0].Model).IsEqualTo(command.Model);
        await Assert.That(controls[0].Exclusive).IsTrue();
    }

    [Test]
    public async Task DeviceMessage_OtaInstall_RoundTrips()
    {
        var version = SemVersion.Parse("1.4.2-beta.1");

        var payload = RoundTrip(DeviceMessage.Create(new DeviceOtaInstallPayload { Version = version })).Payload;

        await Assert.That(payload).IsTypeOf<DeviceOtaInstallPayload>();
        await Assert.That(((DeviceOtaInstallPayload)payload).Version).IsEqualTo(version);
    }

    [Test]
    public async Task DeviceStatus_BoolState_RoundTrips()
    {
        var deviceId = Guid.CreateVersion7();

        var status = RoundTrip(DeviceStatus.Create(deviceId, DeviceBoolStateType.EStopped, true));

        await Assert.That(status.DeviceId).IsEqualTo(deviceId);
        await Assert.That(status.Payload).IsTypeOf<DeviceBoolStatePayload>();
        var state = (DeviceBoolStatePayload)status.Payload;
        await Assert.That(state.Type).IsEqualTo(DeviceBoolStateType.EStopped);
        await Assert.That(state.State).IsTrue();
    }
}
