using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Services.OAuthConnection;

public sealed class OAuthConnectionService : IOAuthConnectionService
{
    private readonly OpenShockContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<OAuthConnectionService> _logger;

    public OAuthConnectionService(OpenShockContext db, IAuditService auditService, ILogger<OAuthConnectionService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public IQueryable<UserOAuthConnection> GetConnectionsAsNonTrackingQuery(Guid userId, CancellationToken cancellationToken)
    {
        return _db.UserOAuthConnections
            .AsNoTracking()
            .Where(c => c.UserId == userId);
    }

    public async Task<UserOAuthConnection?> GetByProviderExternalIdAsync(string provider, string providerAccountId, CancellationToken cancellationToken)
    {
        var p = provider.ToLowerInvariant();
        return await _db.UserOAuthConnections
            .FirstOrDefaultAsync(c => c.ProviderKey == p && c.ExternalId == providerAccountId, cancellationToken);
    }

    public async Task<bool> ConnectionExistsAsync(string provider, string providerAccountId, CancellationToken cancellationToken)
    {
        var p = provider.ToLowerInvariant();
        return await _db.UserOAuthConnections.AnyAsync(c => c.ProviderKey == p && c.ExternalId == providerAccountId, cancellationToken);
    }

    public async Task<bool> HasConnectionAsync(Guid userId, string provider, CancellationToken cancellationToken)
    {
        var p = provider.ToLowerInvariant();
        return await _db.UserOAuthConnections.AnyAsync(c => c.UserId == userId && c.ProviderKey == p, cancellationToken);
    }

    public async Task<bool> TryAddConnectionAsync(Guid userId, string provider, string providerAccountId, string? providerAccountName, Guid? actorId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _db.UserOAuthConnections.Add(new UserOAuthConnection
            {
                UserId = userId,
                ProviderKey = provider.ToLowerInvariant(),
                ExternalId = providerAccountId,
                DisplayName = providerAccountName
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Unique constraint violation (duplicate link)
            _logger.LogDebug(ex, "Duplicate OAuth link for {Provider}:{ExternalId}", provider, providerAccountId);
            return false;
        }

        await _auditService.LogAsync(
            userId,
            action: AuditAction.OAuthConnected,
            actorId: actorId ?? userId,
            metadata: new OAuthConnectedMetadata(provider),
            cancellationToken: cancellationToken
        );

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TryRemoveConnectionAsync(Guid userId, string provider, Guid? actorId, CancellationToken cancellationToken)
    {
        var p = provider.ToLowerInvariant();
        
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        
        var nDeleted = await _db.UserOAuthConnections
            .Where(c => c.UserId == userId && c.ProviderKey == p)
            .ExecuteDeleteAsync(cancellationToken);
        
        // Nothing was linked, so don't record a disconnection that never happened.
        // Disposing the transaction without committing rolls back the (no-op) delete.
        if (nDeleted <= 0) return false;

        await _auditService.LogAsync(
            userId,
            action: AuditAction.OAuthDisconnected,
            actorId: actorId ?? userId,
            metadata: new OAuthDisconnectedMetadata(provider),
            cancellationToken: cancellationToken
        );
        
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}
