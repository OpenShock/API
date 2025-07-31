using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class PublicShareResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    
    public required DateTime CreatedOn { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public required BasicUserInfo Author { get; init; }

    public IList<PublicShareDevice> Devices { get; init; } =
        new List<PublicShareDevice>();
}