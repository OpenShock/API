namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A pending or completed email-change request. The row is created when an authenticated user
/// asks to change their email, and consumed when they follow the verification link sent to the
/// new address. The user's <see cref="User.Email"/> is only updated on consumption.
/// </summary>
public sealed class UserEmailChange
{
    /// <summary>
    /// Internal identifier. Unlike password resets, the email-change link only carries the secret
    /// token — the id is not exposed in the URL.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User this email change belongs to.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// The user's email address at the time the request was created. Preserved for audit; not
    /// part of the verification predicate (the version counter handles that).
    /// </summary>
    public required string OldEmail { get; set; }

    /// <summary>
    /// The email address the user wants to switch to. Written to <see cref="User.Email"/> when
    /// the verification link is followed.
    /// </summary>
    public required string NewEmail { get; set; }

    /// <summary>
    /// Hash of the secret token sent to <see cref="NewEmail"/>. The plaintext token is never
    /// stored — it only exists in the verification email and in the request the user submits.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Snapshot of <see cref="User.EmailVersion"/> at the moment this request was created.
    /// The change is only valid while this still matches the user's current email version, so any
    /// other email change going through first silently invalidates this one.
    /// </summary>
    public required int EmailVersionAtCreate { get; set; }

    /// <summary>
    /// When the verification link was followed and the email was actually changed. Null while the
    /// request is still pending.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// When the request was created. Combined with <see cref="Constants.Duration.EmailChangeRequestLifetime"/>
    /// to enforce the link's expiry.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
