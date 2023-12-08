namespace OpenShock.Common.Redis.PubSub;

public sealed class DeviceUpdatedMessage
{
    public required Guid Id { get; set; }
}