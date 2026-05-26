using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services.Bypass;

public sealed class BypassTokenService : IBypassTokenService
{
    private readonly OpenShockContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BypassTokenService(OpenShockContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResolvedBypassToken?> ResolveAsync(string secret, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length != IBypassTokenService.SecretLength) return null;

        var hash = HashingUtils.HashToken(secret);

        var token = await _db.BypassTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => new { t.Id, t.Types })
            .FirstOrDefaultAsync(ct);

        if (token is null) return null;

        var tokenId = token.Id;
        await _db.BypassTokens
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.LastUsedAt, DateTime.UtcNow)
                .SetProperty(t => t.UseCount, t => t.UseCount + 1), ct);

        return new ResolvedBypassToken(tokenId, token.Types);
    }

    public Task<bool> TryRecordUseAsync(Guid userId, CancellationToken ct)
    {
        var bypass = _httpContextAccessor.HttpContext?.GetResolvedBypassToken();
        if (bypass is null) return Task.FromResult(true);

        return LinkAsync(bypass.Id, userId, ct);
    }

    public async Task<bool> TryRecordUseByEmailAsync(string email, CancellationToken ct)
    {
        var bypass = _httpContextAccessor.HttpContext?.GetResolvedBypassToken();
        if (bypass is null) return true;

        var userId = await _db.Users
            .Where(u => u.Email == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return true;

        return await LinkAsync(bypass.Id, userId.Value, ct);
    }

    private async Task<bool> LinkAsync(Guid bypassTokenId, Guid userId, CancellationToken ct)
    {
        var roles = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Roles)
            .FirstOrDefaultAsync(ct);

        if (roles is null) return true; // user not found — nothing to link; treat as non-admin
        if (roles.Contains(RoleType.Admin)) return false;

        var now = DateTime.UtcNow;

        await _db.BypassTokens
            .Where(t => t.Id == bypassTokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedByUserId, userId), ct);

        const string sql = """
            INSERT INTO bypass_token_user_uses (bypass_token_id, user_id, first_used_at, last_used_at, use_count)
            VALUES ({0}, {1}, {2}, {2}, 1)
            ON CONFLICT (bypass_token_id, user_id)
            DO UPDATE SET last_used_at = EXCLUDED.last_used_at,
                          use_count = bypass_token_user_uses.use_count + 1
            """;

        await _db.Database.ExecuteSqlRawAsync(sql, [bypassTokenId, userId, now], ct);

        return true;
    }
}
