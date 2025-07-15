namespace OpenShock.API.Models.Response;

public sealed class LcgNodeResponseV2
{
    public required string Host { get; init; }
    public required ushort Port { get; init; }
    public required string Path { get; init; }
    public required string Country { get; init; }
}