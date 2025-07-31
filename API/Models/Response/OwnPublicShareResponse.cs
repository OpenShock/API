using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class OwnPublicShareResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedOn { get; init; }
    public DateTime? ExpiresOn { get; init; }

    public static OwnPublicShareResponse GetFromEf(PublicShare x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        CreatedOn = x.CreatedAt,
        ExpiresOn = x.ExpiresAt
    };
}