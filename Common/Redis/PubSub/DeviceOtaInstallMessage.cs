using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Semver;

namespace OpenShock.Common.Redis.PubSub;

public sealed class DeviceOtaInstallMessage
{
    public required Guid Id { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion Version { get; set; }
}