namespace OpenShock.Common.OpenShockDb;

public sealed class AutomationToken
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string TokenHash { get; set; }

    public required List<AutomationTokenType> Types { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? LastRotatedAt { get; set; }

    public long UseCount { get; set; }

    public bool AutoCleanupUsers { get; set; }

    /// <summary>
    /// With <see cref="AutoCleanupUsers"/>, how long after creation the accounts this token created are deleted.
    /// </summary>
    public TimeSpan? AutoCleanupAfter { get; set; }

    // Navigations
    public ICollection<User> CreatedUsers { get; } = [];
}
