namespace ShockLink.API.Models.Response;

public class ShareLinkDevice
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public IList<ShareLinkShocker> Shockers { get; set; } = new List<ShareLinkShocker>();
}