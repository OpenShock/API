using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class PublicShareResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public required GenericIni Author { get; set; }

    public IList<PublicShareDevice> Devices { get; set; } =
        new List<PublicShareDevice>();
}