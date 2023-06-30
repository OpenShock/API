namespace ShockLink.API.Models.Requests;

public class ShareLinkCreate
{
    public required string Name { get; set; }
    public DateTime? ExpiresOn { get; set; } = null;
}