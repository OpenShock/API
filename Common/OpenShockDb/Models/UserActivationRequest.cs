namespace OpenShock.Common.OpenShockDb;

public sealed class UserActivationRequest
{
    public required Guid UserId { get; set; }

    /// <summary>
    /// Hash of the secret activation token. The plaintext is never stored. Seeded when the request is
    /// created and re-minted by the email outbox consumer on every (re)send, so the queue never holds
    /// a usable activation link. See <see cref="EmailOutboxMessage"/>.
    /// </summary>
    public required string TokenHash { get; set; }

    public int EmailSendAttempts { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
