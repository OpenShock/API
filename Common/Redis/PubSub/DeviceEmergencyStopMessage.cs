using System.Text.Json.Serialization;
namespace OpenShock.Common.Redis.PubSub;

public sealed class DeviceEmergencyStopMessage
{
    public required Guid Id { get; set; }
}