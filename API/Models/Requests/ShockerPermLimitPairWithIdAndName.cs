namespace OpenShock.API.Models.Requests;

public sealed class ShockerPermLimitPairWithIdAndName : ShockerPermLimitPairWithId
{
    public required string Name { get; init; }
}