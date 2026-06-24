namespace OpenShock.Common.Services.Email.Queue;

/// <summary>
/// Type-specific JSONB payloads persisted on a <see cref="OpenShock.Common.OpenShockDb.QueuedEmail"/>.
/// These hold only the non-secret data the retry worker needs to look up the account and regenerate a
/// fresh token before resending — never the token or link itself.
/// </summary>
public static class QueuedEmailPayloads
{
    /// <summary>Payload for <see cref="OpenShock.Common.Models.QueuedEmailType.AccountActivation"/>.</summary>
    public sealed record Activation(Guid UserId, string Email);

    /// <summary>Payload for <see cref="OpenShock.Common.Models.QueuedEmailType.EmailVerification"/>.</summary>
    public sealed record EmailVerification(Guid UserId, string Email);

    /// <summary>Payload for <see cref="OpenShock.Common.Models.QueuedEmailType.EmailChangeNotice"/>.</summary>
    public sealed record EmailChangeNotice(Guid UserId, string Email, string NewEmail);
}
