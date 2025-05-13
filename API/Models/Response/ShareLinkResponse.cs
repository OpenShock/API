using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class ShareLinkResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public static ShareLinkResponse GetFromEf(ShockerShareLink x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        CreatedOn = x.CreatedAt,
        ExpiresOn = x.ExpiresAt
    };
}