using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Errors;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Integrations;

public sealed partial class IntegrationsController
{
    /// <summary>
    /// Lets a service holding many users keep their lifetime control stats in sync with a single call per tick.
    /// Returns the lifetime totals of every requested user whose stats changed since the given cursor.
    /// </summary>
    /// <remarks>
    /// Requires an automation token granting <c>ControlStatsRead</c>. Stats rows are hourly buckets that only
    /// ever change while their hour is current, so "changed since" is answered from the bucket indexes. The
    /// lookback is widened by <see cref="FlushOverlap"/> to cover buffered live control stats flushed just after
    /// the hour rolled over, so a user can be reported again although nothing changed. Live control stats that
    /// only get written after a longer database outage land in an older bucket and are missed, the totals catch up
    /// with that user's next control since they are always lifetime totals.
    /// </remarks>
    /// <response code="200">The changed users' totals and the cursor to pass next time.</response>
    /// <response code="403">No automation token granting ControlStatsRead was presented.</response>
    [HttpPost("control-stats/changes")]
    [ProducesResponseType<ControlStatChangesResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // MissingCapability
    public async Task<IActionResult> GetControlStatChanges(ControlStatChangesRequest body, CancellationToken cancellationToken)
    {
        if (!HttpContext.IsBypassed(AutomationTokenType.ControlStatsRead))
            return Problem(ControlStatsError.MissingCapability);

        // Taken before querying, so anything written while we read is picked up by the next call
        var cursor = DateTime.UtcNow;
        var userIds = body.UserIds.Distinct().ToArray();

        Guid[] changed;
        if (body.Since is not null)
        {
            // A cursor without offset is the UTC one we handed out, not server local time
            var since = body.Since.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(body.Since.Value, DateTimeKind.Utc)
                : body.Since.Value.ToUniversalTime();
            var from = ControlStatDelta.ToBucket(since - FlushOverlap);
            var recent = _db.ControlStats.Where(x => x.Bucket >= from);

            var owners = await recent
                .Where(x => userIds.Contains(x.OwnerUserId))
                .Select(x => x.OwnerUserId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            var controllers = await recent
                .Where(x => x.ControllerUserId != null && userIds.Contains(x.ControllerUserId.Value))
                .Select(x => x.ControllerUserId!.Value)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            changed = owners.Union(controllers).ToArray();
        }
        else
        {
            // Initial sync, everyone requested is reported, users without any stats with empty totals
            changed = userIds;
        }

        if (changed.Length == 0)
            return Ok(new ControlStatChangesResponse { Cursor = cursor, Users = [] });

        var received = await _db.ControlStats
            .Where(x => changed.Contains(x.OwnerUserId))
            .GroupBy(x => new { UserId = x.OwnerUserId, x.Type, x.Live })
            .Select(g => new TotalsRow(g.Key.UserId, g.Key.Type, g.Key.Live, g.Sum(x => x.Count),
                g.Sum(x => x.DurationMs), g.Sum(x => x.IntensityMs), g.Max(x => x.MaxIntensity)))
            .ToArrayAsync(cancellationToken);

        var given = await _db.ControlStats
            .Where(x => x.ControllerUserId != null && changed.Contains(x.ControllerUserId.Value))
            .GroupBy(x => new { UserId = x.ControllerUserId!.Value, x.Type, x.Live })
            .Select(g => new TotalsRow(g.Key.UserId, g.Key.Type, g.Key.Live, g.Sum(x => x.Count),
                g.Sum(x => x.DurationMs), g.Sum(x => x.IntensityMs), g.Max(x => x.MaxIntensity)))
            .ToArrayAsync(cancellationToken);

        var receivedByUser = received.ToLookup(x => x.UserId);
        var givenByUser = given.ToLookup(x => x.UserId);

        return Ok(new ControlStatChangesResponse
        {
            Cursor = cursor,
            Users = changed
                .Select(id => new UserControlStatTotals
                {
                    UserId = id,
                    Received = ToTotals(receivedByUser[id]),
                    Given = ToTotals(givenByUser[id])
                })
                .ToArray()
        });
    }

    /// <summary>Well above the batch update service's flush interval, which writes live control stats.</summary>
    private static readonly TimeSpan FlushOverlap = TimeSpan.FromMinutes(5);

    private static ControlStatTotals[] ToTotals(IEnumerable<TotalsRow> rows) => rows
        .OrderBy(x => x.Type)
        .ThenBy(x => x.Live)
        .Select(x => new ControlStatTotals
        {
            Type = x.Type,
            Live = x.Live,
            Count = x.Count,
            DurationMs = x.DurationMs,
            AverageIntensity = x.DurationMs == 0 ? 0 : (double)x.IntensityMs / x.DurationMs,
            MaxIntensity = x.MaxIntensity
        })
        .ToArray();

    private sealed record TotalsRow(
        Guid UserId,
        ControlType Type,
        bool Live,
        long Count,
        long DurationMs,
        long IntensityMs,
        byte MaxIntensity);
}
