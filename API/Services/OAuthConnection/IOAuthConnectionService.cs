using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.OAuthConnection;

/// <summary>
/// Manages external OAuth connections for users.
/// </summary>
public interface IOAuthConnectionService
{
    IQueryable<UserOAuthConnection> GetConnectionsAsNonTrackingQuery(Guid userId, CancellationToken cancellationToken = default);
    Task<UserOAuthConnection?> GetByProviderExternalIdAsync(string provider, string providerAccountId, CancellationToken cancellationToken = default);
    Task<bool> ConnectionExistsAsync(string provider, string providerAccountId, CancellationToken cancellationToken = default);
    Task<bool> HasConnectionAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<bool> TryAddConnectionAsync(Guid userId, string provider, string providerAccountId, string? providerAccountName, Guid? actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the connection of the user to <paramref name="provider"/>, unless it is the only way left to
    /// sign in to the account (no password and no other connection). Removals for one user run one at a
    /// time, so two of them cannot each count the connection the other is removing as the one that remains.
    /// </summary>
    Task<RemoveConnectionResult> RemoveConnectionAsync(Guid userId, string provider, Guid? actorId, CancellationToken cancellationToken = default);
}

public enum RemoveConnectionResult
{
    Removed,
    NotFound,
    LastLoginMethod
}
