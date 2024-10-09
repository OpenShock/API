namespace OpenShock.API.Models.Response;

public sealed class ResponseDeviceWithShockers : ResponseDevice
{
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}