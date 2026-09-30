using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services.AutomationTokens;

public sealed class AutomationTokenService : IAutomationTokenService
{
    private readonly OpenShockContext _db;
    private readonly IBatchUpdateService _batchUpdateService;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AutomationTokenService(
        OpenShockContext db,
        IBatchUpdateService batchUpdateService,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _batchUpdateService = batchUpdateService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
    }

    public ResolvedAutomationToken? Current => _httpContextAccessor.HttpContext?.GetResolvedAutomationToken();

    public async Task<ResolvedAutomationToken?> ResolveAsync(string secret, CancellationToken ct)
    {
        if (secret.Length != IAutomationTokenService.SecretLength) return null;

        var tokenHash = HashingUtils.HashToken(secret);

        return await _db.AutomationTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => new ResolvedAutomationToken(t.Id, t.Name, t.Types))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsApiTokenOwnerPrivilegedAsync(string apiToken, CancellationToken ct)
    {
        var tokenHash = HashingUtils.HashToken(apiToken);

        var roles = await _db.ApiTokens
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => t.User.Roles)
            .FirstOrDefaultAsync(ct);

        return roles is not null && PrivilegedRoles.Any(roles);
    }

    public void RecordRequest(Guid automationTokenId) => _batchUpdateService.UpdateAutomationTokenUsed(automationTokenId);

    public async Task RecordSignupAsync(Guid userId, CancellationToken ct)
    {
        if (Current is not { } automationToken) return;

        await AuditUseAsync(automationToken, userId, AutomationTokenFlow.Signup, ct);
    }

    public async Task<bool> CanUseForLoginAsync(string usernameOrEmail, CancellationToken ct)
    {
        if (Current is null) return true;

        // Same lookup as AccountService.GetAccountByCredentialsAsync
        var lowercaseUsernameOrEmail = usernameOrEmail.ToLowerInvariant();
        var roles = await _db.Users
            .Where(u => u.Email == lowercaseUsernameOrEmail || u.Name == lowercaseUsernameOrEmail)
            .Select(u => u.Roles)
            .FirstOrDefaultAsync(ct);

        return roles is null || !PrivilegedRoles.Any(roles);
    }

    public async Task<bool> TryRecordUseAsync(Guid userId, AutomationTokenFlow flow, CancellationToken ct)
    {
        if (Current is not { } automationToken) return true;

        if (await IsPrivilegedAsync(userId, ct)) return false;

        await AuditUseAsync(automationToken, userId, flow, ct);

        return true;
    }

    public async Task<bool> TryRecordUseByEmailAsync(string email, AutomationTokenFlow flow, CancellationToken ct)
    {
        if (Current is null) return true;

        email = email.ToLowerInvariant();
        var userId = await _db.Users
            .Where(u => u.Email == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return true;

        return await TryRecordUseAsync(userId.Value, flow, ct);
    }

    private async Task<bool> IsPrivilegedAsync(Guid userId, CancellationToken ct)
    {
        var roles = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Roles)
            .FirstOrDefaultAsync(ct);

        return roles is not null && PrivilegedRoles.Any(roles);
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
