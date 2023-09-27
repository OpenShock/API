namespace OpenShock.API.Models.Response;

public class ShockerWithDevice : ShockerResponse
{
    public required Guid Device { get; set; }
}