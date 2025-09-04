using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.Account;

public sealed class OAuthConnectionService : IOAuthConnectionService
{
    private readonly OpenShockContext _db;
    private readonly ILogger<OAuthConnectionService> _logger;

    public OAuthConnectionService(OpenShockContext db, ILogger<OAuthConnectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserOAuthConnection[]> GetConnectionsAsync(Guid userId)
    {
        return await _db.UserOAuthConnections
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToArrayAsync();
    }

    public async Task<UserOAuthConnection?> GetByProviderExternalIdAsync(string provider, string providerAccountId)
    {
        var p = provider.ToLowerInvariant();
        return await _db.UserOAuthConnections
            .FirstOrDefaultAsync(c => c.ProviderKey == p && c.ExternalId == providerAccountId);
    }

    public async Task<bool> HasConnectionAsync(Guid userId, string provider)
    {
        var p = provider.ToLowerInvariant();
        return await _db.UserOAuthConnections.AnyAsync(c => c.UserId == userId && c.ProviderKey == p);
    }

    public async Task<bool> TryAddConnectionAsync(Guid userId, string provider, string providerAccountId, string? providerAccountName)
    {
        try
        {
            _db.UserOAuthConnections.Add(new UserOAuthConnection
            {
                UserId = userId,
                ProviderKey = provider.ToLowerInvariant(),
                ExternalId = providerAccountId,
                DisplayName = providerAccountName
            });
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Unique constraint violation (duplicate link)
            _logger.LogDebug(ex, "Duplicate OAuth link for {Provider}:{ExternalId}", provider, providerAccountId);
            return false;
        }
    }

    public async Task<bool> TryRemoveConnectionAsync(Guid userId, string provider)
    {
        var p = provider.ToLowerInvariant();
        var nDeleted = await _db.UserOAuthConnections
            .Where(c => c.UserId == userId && c.ProviderKey == p)
            .ExecuteDeleteAsync();

        return nDeleted > 0;
    }
}
