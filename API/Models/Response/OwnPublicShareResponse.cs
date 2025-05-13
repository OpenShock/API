using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class OwnPublicShareResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public static OwnPublicShareResponse GetFromEf(PublicShare x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        CreatedOn = x.CreatedAt,
        ExpiresOn = x.ExpiresAt
    };
}