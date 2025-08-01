using System.Text.Json.Serialization;
namespace OpenShock.Common.Redis.PubSub;

public sealed class DeviceRebootMessage
{
    public required Guid Id { get; set; }
}