using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.ApiVersioning;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklist/emailProviders")]
    [ApiVersion(1)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddEmailProviderBlacklist([FromBody] AddEmailProviderBlacklistDto body, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var domains = body.Domains
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Select(domain => domain.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        // The unique index ignores case, so domains that are already listed are skipped by the database
        await _db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO email_provider_blacklist (id, domain)
             SELECT gen_random_uuid(), domain FROM unnest({domains}) AS domain
             ON CONFLICT DO NOTHING
             """, ct);

        return Ok();
    }
}
