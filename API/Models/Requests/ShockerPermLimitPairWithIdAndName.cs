namespace OpenShock.API.Models.Requests;

public class ShockerPermLimitPairWithIdAndName : ShockerPermLimitPairWithId
{
    public required string Name { get; set; }
}