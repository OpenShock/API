using ShockLink.Common.Models;

namespace ShockLink.API.Models.Response;

public class PublicShareLinkResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public required GenericIni Author { get; set; }

    public IList<OwnerShockerResponse.SharedDevice> Devices { get; set; } =
        new List<OwnerShockerResponse.SharedDevice>();
}