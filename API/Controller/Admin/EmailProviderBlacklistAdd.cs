using System.Globalization;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Constants;
using OpenShock.Common.Problems.CustomProblems;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Adds domains to the email provider blacklist. Every entry is validated first; if any is invalid, nothing is
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

            if (TryNormalizeEmailProviderDomain(entry, out var domain))
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

    /// <summary>
    /// Turns a pasted entry into the domain that signup compares against. A full address contributes its domain.
    /// The result has to be something an email address's host can equal, otherwise the entry would never block
    /// anything, so it is checked by parsing an address on that domain the same way signup does.
    /// </summary>
    private static bool TryNormalizeEmailProviderDomain(string entry, out string domain)
    {
        domain = entry.Trim();

        var at = domain.LastIndexOf('@');
        if (at >= 0) domain = domain[(at + 1)..];

        domain = domain.TrimEnd('.').ToLowerInvariant();

        if (domain.Length is 0 or > ApiHardLimits.EmailProviderDomainMaxLength) return false;

        if (!IsValidMailDomain(domain)) return false;

        return MailAddress.TryCreate($"user@{domain}", out var address)
               && string.Equals(address.Host, domain, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A mail domain per RFC 5321: at least two labels (no bare "localhost"), each 1 to 63 letters, digits or
    /// hyphens, not starting or ending with a hyphen. International domains are checked in their punycode form,
    /// and an all-numeric top label is rejected so IPv4 literals don't pass.
    /// </summary>
    private static bool IsValidMailDomain(string domain)
    {
        string ascii;
        try
        {
            ascii = new IdnMapping { UseStd3AsciiRules = true }.GetAscii(domain);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (ascii.Length > ApiHardLimits.EmailProviderDomainMaxLength) return false;

        var labels = ascii.Split('.');
        if (labels.Length < 2) return false;

        foreach (var label in labels)
        {
            if (label.Length is 0 or > 63 || label[0] == '-' || label[^1] == '-') return false;
            if (!label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) return false;
        }

        return !labels[^1].All(char.IsAsciiDigit);
    }
}
