namespace OpenShock.API.Models.Requests;

public sealed class ShareLinkCreate
{
    public required string Name { get; set; }
    public DateTime? ExpiresOn { get; set; } = null;
}