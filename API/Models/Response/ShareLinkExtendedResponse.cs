namespace ShockLink.API.Models.Response;

public class ShareLinkExtendedResponse : ShareLinkResponse
{
    public required IEnumerable<OwnerShockerResponse.SharedDevice> Devices { get; set; }
}