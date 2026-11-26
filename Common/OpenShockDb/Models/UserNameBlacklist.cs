namespace OpenShock.Common.OpenShockDb;

public sealed class UserNameBlacklist
{
    public Guid Id { get; init; }

    public required string Value { get; init; } = null!;

    public required MatchTypeEnum MatchType { get; init; }
    
    public DateTime CreatedAt { get; init; }
}
