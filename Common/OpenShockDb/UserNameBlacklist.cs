namespace OpenShock.Common.OpenShockDb;

public enum MatchType
{
    Exact,
    Contains,
}

public sealed class UserNameBlacklist
{
    public required Guid Id { get; set; }

    public string Value { get; set; } = null!;

    public MatchType MatchType { get; set; }
    
    public DateTime CreatedAt { get; set; }
}