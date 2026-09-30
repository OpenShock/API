using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AutomationTokenDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<AutomationTokenType> Types { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime? LastRotatedAt { get; init; }
    public long UseCount { get; init; }
    public bool AutoCleanupUsers { get; init; }
    public TimeSpan? AutoCleanupAfter { get; init; }

    public static AutomationTokenDto FromEntity(AutomationToken token) => new()
    {
        Id = token.Id,
        Name = token.Name,
        Types = token.Types,
        CreatedAt = token.CreatedAt,
        LastUsedAt = token.LastUsedAt,
        LastRotatedAt = token.LastRotatedAt,
        UseCount = token.UseCount,
        AutoCleanupUsers = token.AutoCleanupUsers,
        AutoCleanupAfter = token.AutoCleanupAfter,
    };
}

public sealed class CreatedAutomationTokenDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Secret { get; init; }
    public required IReadOnlyList<AutomationTokenType> Types { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastRotatedAt { get; init; }
    public bool AutoCleanupUsers { get; init; }
    public TimeSpan? AutoCleanupAfter { get; init; }
}
