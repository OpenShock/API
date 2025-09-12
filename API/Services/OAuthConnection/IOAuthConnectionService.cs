using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.OAuthConnection;

/// <summary>
/// Manages external OAuth connections for users.
/// </summary>
public interface IOAuthConnectionService
{
    Task<UserOAuthConnection[]> GetConnectionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserOAuthConnection?> GetByProviderExternalIdAsync(string provider, string providerAccountId, CancellationToken cancellationToken = default);
    Task<bool> ConnectionExistsAsync(string provider, string providerAccountId, CancellationToken cancellationToken = default);
    Task<bool> HasConnectionAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<bool> TryAddConnectionAsync(Guid userId, string provider, string providerAccountId, string? providerAccountName, CancellationToken cancellationToken = default);
    Task<bool> TryRemoveConnectionAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
}