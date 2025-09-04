using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.OAuthConnection;

/// <summary>
/// Manages external OAuth connections for users.
/// </summary>
public interface IOAuthConnectionService
{
    Task<UserOAuthConnection[]> GetConnectionsAsync(Guid userId);
    Task<UserOAuthConnection?> GetByProviderExternalIdAsync(string provider, string providerAccountId);
    Task<bool> HasConnectionAsync(Guid userId, string provider);
    Task<bool> TryAddConnectionAsync(Guid userId, string provider, string providerAccountId, string? providerAccountName);
    Task<bool> TryRemoveConnectionAsync(Guid userId, string provider);
}