namespace OpenShock.API.Models.Response;

public sealed class LcgNodeResponse
{
    public required string Fqdn { get; init; }
    public required string Country { get; init; }
}