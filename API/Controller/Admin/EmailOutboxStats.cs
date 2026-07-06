using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;
using System.Net.Mime;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Returns the number of email outbox messages in each delivery state.
    /// </summary>
    /// <response code="200">Per-status counts</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("email/outbox/stats")]
    [ProducesResponseType<EmailOutboxStatsDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<EmailOutboxStatsDto> GetEmailOutboxStats()
    {
        var counts = await _db.EmailOutbox
            .AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        long Count(EmailStatus status) => counts.GetValueOrDefault(status);

        return new EmailOutboxStatsDto
        {
            Pending = Count(EmailStatus.Pending),
            Sending = Count(EmailStatus.Sending),
            Sent = Count(EmailStatus.Sent),
            Failed = Count(EmailStatus.Failed),
            Skipped = Count(EmailStatus.Skipped),
            Total = counts.Values.Sum()
        };
    }
}
