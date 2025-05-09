namespace OpenShock.API.Models.Response;

public sealed class ShareLinkDevice
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required IList<ShareLinkShocker> Shockers { get; set; }
}