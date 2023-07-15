namespace ShockLink.API.Models.Response;

public class ShareLinkWithShockersResponse : ShareLinkResponse
{
    public required IEnumerable<OwnerShockerResponse.SharedDevice.SharedShocker> Shockers { get; set; }
}