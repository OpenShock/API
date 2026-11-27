using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Validation;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Lists the email provider blacklist. With <paramref name="match"/> (a domain or an email address), returns only
    /// the entry that would block signups on that domain, using the same comparison signup does.
    /// </summary>
    [HttpGet("blacklist/emailProviders")]
    [ApiVersion(1)]
    public async IAsyncEnumerable<EmailProviderBlacklistDto> ListEmailProviderBlacklist([FromQuery(Name="match")] string? match, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        PedanticallyEnsureAdmin();

        var query = _db.EmailProviderBlacklists.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(match))
        {
            if (!EmailValidator.TryNormalizeDomain(match, out var domain)) yield break;

            query = query.Where(x => x.Domain == domain);
        }

        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return new EmailProviderBlacklistDto
            {
                Id = item.Id,
                Domain = item.Domain,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
