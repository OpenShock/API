using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Semver;

namespace OpenShock.Common.Redis.PubSub;

public sealed class DeviceEmergencyStopMessage
{
    public required Guid Id { get; set; }
}