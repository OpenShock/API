namespace OpenShock.API.Models.Response;

public sealed class DeviceWithShockersResponse : DeviceResponse
{
    public required ShockerResponse[] Shockers { get; init; }
}