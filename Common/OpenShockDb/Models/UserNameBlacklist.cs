namespace OpenShock.Common.OpenShockDb;

public sealed class UserNameBlacklist
{
    public required Guid Id { get; init; }

    public required string Value { get; init; } = null!;

    public required MatchTypeEnum MatchType { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public bool IsMatch(string value) => MatchType switch
    {
        MatchTypeEnum.Exact => value.Equals(Value, StringComparison.InvariantCultureIgnoreCase),
        MatchTypeEnum.Contains => value.Contains(Value, StringComparison.InvariantCultureIgnoreCase),
        _ => false
    };
}