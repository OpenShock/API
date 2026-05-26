using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class BypassToken
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string TokenHash { get; set; }

    public required List<BypassTokenType> Types { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public Guid? LastUsedByUserId { get; set; }

    public DateTime? LastRotatedAt { get; set; }

    public long UseCount { get; set; }

    public bool AutoCleanupUsers { get; set; }

    public TimeSpan? AutoCleanupAfter { get; set; }

    // Navigations
    public User? LastUsedByUser { get; set; }
    public ICollection<BypassTokenUserUse> UserUses { get; } = [];
}
