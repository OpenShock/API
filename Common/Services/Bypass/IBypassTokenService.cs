using OpenShock.Common.Models;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services.Bypass;

public interface IBypassTokenService
{
    /// <summary>
    /// Length (in chars) of the opaque random secret. The full secret is base62-style alphanumeric.
    /// </summary>
    const int SecretLength = 128;

    /// <summary>
    /// Generates a new opaque bypass-token secret. The secret is sent via the
    /// <c>X-OpenShock-Bypass-Token</c> header — it has no prefix because it lives in a dedicated
    /// header and is not confused with any other token.
    /// </summary>
    static string GenerateSecret() => CryptoUtils.RandomAlphaNumericString(SecretLength);

    /// <summary>
    /// Hashes the supplied secret and looks it up. On a match, increments the global UseCount and
    /// LastUsedAt and returns the resolved token. Called once per request by the bypass middleware.
    /// </summary>
    Task<ResolvedBypassToken?> ResolveAsync(string secret, CancellationToken ct);

    /// <summary>
    /// If the current request resolved a bypass token, associates the use with the given user account.
    /// Returns <c>false</c> ONLY when a bypass was used AND the target user has the
    /// <see cref="RoleType.Admin"/> role — callers MUST treat that as "bypass not honored" and
    /// reject the request, matching the leak-defense contract. Returns <c>true</c> when no bypass
    /// was used, when the user doesn't exist, or when the link was recorded.
    /// </summary>
    Task<bool> TryRecordUseAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Same contract as <see cref="TryRecordUseAsync"/> but resolves the user by email first.
    /// If no matching user exists, this is a no-op and returns <c>true</c> so the caller does not
    /// expose user-existence by behaving differently — relevant on flows like password-reset initiation.
    /// </summary>
    Task<bool> TryRecordUseByEmailAsync(string email, CancellationToken ct);
}
