namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Builders for the opaque <see cref="EmailOutboxMessage.CoalesceKey"/>. The domain that enqueues a
/// message decides whether older requests of the same kind should be superseded by a newer one, and
/// expresses that purely by the key it stamps (or by leaving it <c>null</c>). The delivery queue never
/// parses these strings - it only compares them - so the "always deliver vs newest-wins" policy lives
/// here on the domain side, never in the queue.
/// </summary>
/// <remarks>
/// The kind prefix is baked into the key so different email kinds for the same user never coalesce
/// into each other (a pending reset must not suppress a pending activation). Scope is per user: only
/// the newest activation / reset / verification for a given user is delivered.
/// <see cref="Models.EmailType.EmailChangeNotice"/> intentionally has no builder - it is an
/// always-deliver type, so it carries a <c>null</c> key.
/// </remarks>
public static class EmailOutboxCoalesceKeys
{
    /// <summary>Newest account-activation email per user wins.</summary>
    public static string AccountActivation(Guid userId) => $"activation:{userId}";

    /// <summary>Newest password-reset email per user wins.</summary>
    public static string PasswordReset(Guid userId) => $"pwreset:{userId}";

    /// <summary>Newest email-change verification per user wins.</summary>
    public static string EmailVerification(Guid userId) => $"emailchange:{userId}";
}
