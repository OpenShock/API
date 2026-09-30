using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services.AutomationTokens;

public sealed class AutomationTokenService : IAutomationTokenService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private const string TokensCacheKey = "automation-tokens";
    private const string UserAllowedCacheKey = "automation-token-user-allowed";

    private readonly OpenShockContext _db;
    private readonly IMemoryCache _cache;
    private readonly IBatchUpdateService _batchUpdateService;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AutomationTokenService(
        OpenShockContext db,
        IMemoryCache cache,
        IBatchUpdateService batchUpdateService,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _batchUpdateService = batchUpdateService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResolvedAutomationToken?> ResolveAsync(string secret, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length != IAutomationTokenService.SecretLength) return null;

        var tokens = await GetTokensAsync(ct);

        return tokens.GetValueOrDefault(HashingUtils.HashToken(secret));
    }

    private async Task<FrozenDictionary<string, ResolvedAutomationToken>> GetTokensAsync(CancellationToken ct)
    {
        var tokens = await _cache.GetOrCreateAsync(TokensCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var rows = await _db.AutomationTokens
                .AsNoTracking()
                .Select(t => new { t.Id, t.Name, t.TokenHash, t.Types })
                .ToListAsync(ct);

            return rows.ToFrozenDictionary(
                t => t.TokenHash,
                t => new ResolvedAutomationToken(t.Id, t.Name, t.Types),
                StringComparer.Ordinal);
        });

        return tokens!;
    }

    public void InvalidateCache() => _cache.Remove(TokensCacheKey);

    public void RecordRequest(Guid automationTokenId) => _batchUpdateService.UpdateAutomationTokenUsed(automationTokenId);

    public async Task<bool> IsAllowedForUserAsync(Guid userId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync((UserAllowedCacheKey, userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            // The same roles AccountService.DeleteAccountAsync treats as privileged.
            return !await _db.Users.AnyAsync(u =>
                u.Id == userId &&
                u.Roles.Any(r => r == RoleType.Staff || r == RoleType.Admin || r == RoleType.System), ct);
        });
    }

    public async Task RecordSignupAsync(Guid userId, CancellationToken ct)
    {
        var automationToken = _httpContextAccessor.HttpContext?.GetResolvedAutomationToken();
        if (automationToken is null) return;

        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedByAutomationTokenId, automationToken.Id), ct);

        await AuditUseAsync(automationToken, userId, AutomationTokenFlow.Signup, ct);
    }

    public async Task<bool> CanUseForLoginAsync(string usernameOrEmail, CancellationToken ct)
    {
        var automationToken = _httpContextAccessor.HttpContext?.GetResolvedAutomationToken();
        if (automationToken is null) return true;

        // Same lookup as AccountService.GetAccountByCredentialsAsync
        var lowercaseUsernameOrEmail = usernameOrEmail.ToLowerInvariant();
        var userId = await _db.Users
            .Where(u => u.Email == lowercaseUsernameOrEmail || u.Name == lowercaseUsernameOrEmail)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        return userId is null || await IsAllowedForUserAsync(userId.Value, ct);
    }

    public async Task<bool> TryRecordUseAsync(Guid userId, AutomationTokenFlow flow, CancellationToken ct)
    {
        var automationToken = _httpContextAccessor.HttpContext?.GetResolvedAutomationToken();
        if (automationToken is null) return true;

        if (!await IsAllowedForUserAsync(userId, ct)) return false;

        await AuditUseAsync(automationToken, userId, flow, ct);

        return true;
    }

    public async Task<bool> TryRecordUseByEmailAsync(string email, AutomationTokenFlow flow, CancellationToken ct)
    {
        var automationToken = _httpContextAccessor.HttpContext?.GetResolvedAutomationToken();
        if (automationToken is null) return true;

        email = email.ToLowerInvariant();
        var userId = await _db.Users
            .Where(u => u.Email == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return true;

        return await TryRecordUseAsync(userId.Value, flow, ct);
    }

    private Task AuditUseAsync(ResolvedAutomationToken automationToken, Guid userId, AutomationTokenFlow flow, CancellationToken ct) =>
        _auditService.LogAsync(
            userId,
            action: AuditAction.AutomationTokenUsed,
            // A password reset request proves nothing about who is asking, so it has no actor. In every
            // other flow the caller is the account itself, or has just created it.
            actorId: flow == AutomationTokenFlow.PasswordReset ? null : userId,
            metadata: new AutomationTokenUsedMetadata(automationToken.Id, automationToken.Name, flow.ToString()),
            cancellationToken: ct);
}
