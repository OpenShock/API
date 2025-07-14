namespace OpenShock.API.Models.Response;

public sealed class LcgNodeResponseV2
{
    public required string Host { get; set; }
    public required ushort Port { get; set; }
    public required string Path { get; set; }
    public required string Country { get; set; }
}