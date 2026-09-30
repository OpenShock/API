namespace OpenShock.API.Models.Response;

public sealed class DeviceWithTokenResponse : DeviceResponse
{
    public required string? Token { get; init; }
}