namespace OpenShock.Common.OpenShockDb;

public enum MatchTypeEnum
{
    Exact,
    Contains,
}

public sealed class UserNameBlacklist
{
    public required Guid Id { get; set; }

    public string Value { get; set; } = null!;

    public MatchTypeEnum MatchType { get; set; }
    
    public DateTime CreatedAt { get; set; }
}