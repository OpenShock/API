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
    /// Requires an automation token granting <c>ControlStatsRead</c>. "Changed since" is answered from when each
    /// stats row was last written rather than the time it covers, so live control stats written late (e.g. after a
    /// database outage) are still picked up. Rows deleted along with their owner lower the totals of everyone who
    /// controlled them without leaving a row behind, those are picked up from <see cref="ControlStatRemoval"/>. The
    /// lookback is widened by <see cref="ChangeOverlap"/> to cover writes still committing while the previous call
    /// read, so a user can be reported again although nothing changed.
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
            // A cursor at the very beginning of time can't be widened any further
            var from = since - DateTime.MinValue > ChangeOverlap
                ? since - ChangeOverlap
                : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var recent = _db.ControlStats.Where(x => x.UpdatedAt >= from);

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

            // Deletions leave no row to be updated, so they are recorded separately
            var removed = await _db.ControlStatRemovals
                .Where(x => x.RemovedAt >= from && userIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .ToArrayAsync(cancellationToken);

            changed = owners.Union(controllers).Union(removed).ToArray();
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

    /// <summary>
    /// Well above how long a stats write takes to commit, and any clock skew between the API and the database, since
    /// <see cref="ControlStat.UpdatedAt"/> is stamped by the database when its transaction starts.
    /// </summary>
    private static readonly TimeSpan ChangeOverlap = TimeSpan.FromMinutes(5);

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
