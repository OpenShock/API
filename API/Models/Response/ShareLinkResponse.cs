using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public class ShareLinkResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public static ShareLinkResponse GetFromEf(ShockerSharesLink x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        CreatedOn = x.CreatedOn,
        ExpiresOn = x.ExpiresOn
    };
}