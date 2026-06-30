using OpenShock.Common.Constants;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A pending or completed password reset request, created when a user (or someone with knowledge
/// of their email) initiates the reset flow. The row is consumed when the user follows the
/// emailed link and submits a new password.
/// </summary>
public sealed class UserPasswordReset
{
    /// <summary>
    /// Public identifier embedded in the reset URL alongside the secret token.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User this reset request belongs to.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Hash of the secret token for the reset link. The plaintext token is never stored — it only
    /// exists in the email link and in the request the user submits. Seeded when the request is
    /// created and re-minted by the email outbox consumer on every (re)send, so the queue never
    /// holds a usable reset link and a resend always supersedes earlier links. See <see cref="EmailOutboxMessage"/>.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Snapshot of <see cref="User.SecurityStamp"/> at the moment this request was created.
    /// The reset is only valid while this still matches the user's current stamp, so any
    /// password (or email) change through any path silently invalidates every outstanding
    /// reset link.
    /// </summary>
    public required Guid SecurityStampAtCreate { get; set; }

    /// <summary>
    /// When the reset was consumed. Null while the request is still pending.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// When the reset was created. Combined with <see cref="Duration.PasswordResetRequestLifetime"/>
    /// to enforce the link's expiry.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
