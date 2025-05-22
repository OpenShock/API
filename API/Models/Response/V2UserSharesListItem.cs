namespace OpenShock.API.Models.Response;

public sealed class V2UserSharesListItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Uri Image { get; set; }
    public required IEnumerable<UserShareInfo> Shares { get; init; }
}