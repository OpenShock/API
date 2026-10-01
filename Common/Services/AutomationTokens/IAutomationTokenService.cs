using OpenShock.Common.OpenShockDb;
using OpenShock.Internal.Common.Utils;

namespace OpenShock.Common.Services.AutomationTokens;

/// <summary>
/// An automation token may be used with any account except privileged ones (see <see cref="PrivilegedRoles"/>).
/// Every use in an account flow is written to that account's audit log. Accounts the token itself created are
/// linked to it, which is what makes them automated accounts; only those are ever eligible for auto-cleanup,
/// and they are deleted with the token.
/// </summary>
public interface IAutomationTokenService
{
    /// <summary>
    /// Length (in chars) of the opaque random secret. The full secret is base62-style alphanumeric.
    /// </summary>
    const int SecretLength = 128;

    /// <summary>
    /// Generates a new opaque automation-token secret. The secret is sent via the
    /// <c>X-OpenShock-Automation-Token</c> header. It has no prefix because it lives in a dedicated
    /// header and is not confused with any other token.
    /// </summary>
    static string GenerateSecret() => CryptoUtils.RandomString(SecretLength);

    /// <summary>
    /// The automation token the current request resolved, or <c>null</c> if it presented none (or one
    /// that was refused).
    /// </summary>
    ResolvedAutomationToken? Current { get; }

    /// <summary>
    /// Looks up the token the supplied secret belongs to. Secrets of the wrong length are rejected
    /// without a database round trip.
    /// </summary>
    Task<ResolvedAutomationToken?> ResolveAsync(string secret, CancellationToken ct);

    /// <summary>
    /// True if <paramref name="apiToken"/> belongs to a privileged account. Used before the API-token
    /// scheme has authenticated the request, so the token itself is not validated here.
    /// </summary>
    Task<bool> IsApiTokenOwnerPrivilegedAsync(string apiToken, CancellationToken ct);

    /// <summary>
    /// True if <paramref name="userId"/> holds a privileged role.
    /// </summary>
    Task<bool> IsUserPrivilegedAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Counts one accepted request against the token. Batched, never written per request.
    /// </summary>
    void RecordRequest(ResolvedAutomationToken automationToken);

    /// <summary>
    /// If the current request resolved an automation token, audits its use to create <paramref name="userId"/>.
    /// The account itself is linked to the token when it is created.
    /// </summary>
    Task RecordSignupAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Checked before the password is verified, so an automation token (which also lifts Turnstile and rate
    /// limits) can never be used to guess passwords of privileged accounts. Returns <c>true</c> when
    /// no automation token was used or no such account exists; the normal credential check handles the latter.
    /// </summary>
    Task<bool> CanUseForLoginAsync(string usernameOrEmail, CancellationToken ct);

    /// <summary>
    /// If the current request resolved an automation token, audits a use against <paramref name="userId"/>.
    /// Returns <c>false</c> ONLY when an automation token was used AND the account is privileged. Callers MUST treat
    /// that as "bypass not honored" and reject the request.
    /// </summary>
    Task<bool> TryRecordUseAsync(Guid userId, AutomationTokenFlow flow, CancellationToken ct);

    /// <summary>
    /// Same contract as <see cref="TryRecordUseAsync"/> but resolves the user by email first.
    /// If no matching user exists, this is a no-op and returns <c>true</c> so the caller does not
    /// expose user-existence by behaving differently, which matters on flows like password-reset initiation.
    /// </summary>
    Task<bool> TryRecordUseByEmailAsync(string email, AutomationTokenFlow flow, CancellationToken ct);
}
