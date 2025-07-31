namespace OpenShock.API.Models.Response;

public sealed class V2UserSharesListItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Uri Image { get; init; }
    public required IEnumerable<UserShareInfo> Shares { get; init; }
}