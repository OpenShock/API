namespace OpenShock.Common.OpenShockDb;

public enum MatchTypeEnum
{
    Exact,
    Contains,
}

public sealed class UserNameBlacklist
{
    public required Guid Id { get; set; }

    public required string Value { get; set; } = null!;

    public required MatchTypeEnum MatchType { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsMatch(string value) => MatchType switch
    {
        MatchTypeEnum.Exact => value.Equals(Value, StringComparison.InvariantCultureIgnoreCase),
        MatchTypeEnum.Contains => value.Contains(Value, StringComparison.InvariantCultureIgnoreCase),
        _ => false
    };
}