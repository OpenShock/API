using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Bypass;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("bypassTokens")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<CreatedBypassTokenDto>(StatusCodes.Status200OK)]
    public async Task<CreatedBypassTokenDto> CreateBypassToken([FromBody] CreateBypassTokenDto body, CancellationToken ct)
    {
        var secret = IBypassTokenService.GenerateSecret();
        var token = new BypassToken
        {
            Id = Guid.CreateVersion7(),
            Name = body.Name.Trim(),
            TokenHash = HashingUtils.HashToken(secret),
            Types = [.. body.Types.Distinct()],
            AutoCleanupUsers = body.AutoCleanupUsers,
            AutoCleanupAfter = body.AutoCleanupAfter,
        };

        _db.BypassTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        return new CreatedBypassTokenDto
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
