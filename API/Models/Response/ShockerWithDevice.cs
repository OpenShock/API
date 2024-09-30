namespace OpenShock.API.Models.Response;

public sealed class ShockerWithDevice : ShockerResponse
{
    public required Guid Device { get; set; }
}