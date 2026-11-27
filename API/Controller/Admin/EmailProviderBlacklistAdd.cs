using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Problems.CustomProblems;
using OpenShock.Common.Validation;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Adds domains to the email provider blacklist. Entries may be domains or full addresses, see
    /// <see cref="EmailValidator.TryNormalizeDomain"/>. Every entry is validated first; if any is invalid, nothing is
    /// added and the invalid entries are returned. The valid ones are then inserted in a single statement.
    /// </summary>
    [HttpPost("blacklist/emailProviders")]
    [ApiVersion(1)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<AddEmailProviderBlacklistResultDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<InvalidEmailProviderDomainsProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> AddEmailProviderBlacklist([FromBody] AddEmailProviderBlacklistDto body, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var domains = new HashSet<string>(StringComparer.Ordinal);
        var invalid = new List<string>();

        foreach (var entry in body.Domains)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            if (EmailValidator.TryNormalizeDomain(entry, out var domain))
                domains.Add(domain);
            else
                invalid.Add(entry);
        }

        if (invalid.Count > 0) return Problem(new InvalidEmailProviderDomainsProblem(invalid));

        var added = 0;
        if (domains.Count > 0)
        {
            // One array parameter however many domains there are. The unique index ignores case, so domains that
            // are already listed are skipped by the database.
            var domainArray = domains.ToArray();
            added = await _db.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO email_provider_blacklist (id, domain)
                 SELECT gen_random_uuid(), domain FROM unnest({domainArray}) AS domain
                 ON CONFLICT DO NOTHING
                 """, ct);
        }

        return Ok(new AddEmailProviderBlacklistResultDto { Submitted = domains.Count, Added = added });
    }
}
