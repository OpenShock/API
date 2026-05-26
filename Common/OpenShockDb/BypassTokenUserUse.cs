namespace OpenShock.Common.OpenShockDb;

public sealed class BypassTokenUserUse
{
    public required Guid BypassTokenId { get; set; }

    public required Guid UserId { get; set; }

    public DateTime FirstUsedAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public long UseCount { get; set; }

    // Navigations
    public BypassToken BypassToken { get; set; } = null!;
    public User User { get; set; } = null!;
}
