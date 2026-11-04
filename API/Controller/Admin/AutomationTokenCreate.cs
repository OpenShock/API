using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Creates an automation token. The secret is only returned once, in this response
    /// </summary>
    [HttpPost("automationTokens")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<CreatedAutomationTokenDto>(StatusCodes.Status200OK)]
    public async Task<CreatedAutomationTokenDto> CreateAutomationToken(
        [FromBody] CreateAutomationTokenDto body,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var secret = IAutomationTokenService.GenerateSecret();
        var token = new AutomationToken
        {
            Id = Guid.CreateVersion7(),
            Name = body.Name.Trim(),
            TokenHash = HashingUtils.HashToken(secret),
            Types = [.. body.Types.Distinct()],
            AutoCleanupUsers = body.AutoCleanupUsers,
            AutoCleanupAfter = body.AutoCleanupAfter,
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        _db.AutomationTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenCreated,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenCreatedMetadata(token.Id, token.Name, [.. token.Types.Select(t => t.ToString())], token.AutoCleanupUsers, token.AutoCleanupAfter),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        return new CreatedAutomationTokenDto
        {
            Id = token.Id,
            Name = token.Name,
            Secret = secret,
            Types = token.Types,
            CreatedAt = token.CreatedAt,
            AutoCleanupUsers = token.AutoCleanupUsers,
            AutoCleanupAfter = token.AutoCleanupAfter,
        };
    }
}
