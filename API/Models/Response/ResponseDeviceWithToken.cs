namespace OpenShock.API.Models.Response;

public sealed class ResponseDeviceWithToken : ResponseDevice
{
    public required string? Token { get; init; }
}