using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class PublicShareLinkResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public required GenericIni Author { get; set; }

    public IList<ShareLinkDevice> Devices { get; set; } =
        new List<ShareLinkDevice>();
}