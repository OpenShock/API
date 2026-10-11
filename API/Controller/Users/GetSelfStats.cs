using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Errors;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Get control statistics for the current user, both for their own shockers and for controls they sent.
    /// </summary>
    /// <param name="tz">IANA time zone the daily breakdown and the date range are in, defaults to UTC.</param>
    /// <param name="from">First day to include (inclusive), defaults to all time.</param>
    /// <param name="to">Last day to include (inclusive), defaults to today.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">The statistics.</response>
    /// <response code="400">Unknown time zone or invalid range.</response>
    [HttpGet("self/stats")]
    [ProducesResponseType<ControlStatsResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // InvalidTimeZone, InvalidRange
    public async Task<IActionResult> GetSelfStats(
        [FromQuery, MaxLength(64)] string? tz,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        tz ??= "UTC";
        if (from > to) return Problem(ControlStatsError.InvalidRange);

        // Postgres is the one interpreting the time zone, so it is the one validating it. Done upfront since it
        // only evaluates AT TIME ZONE once there are rows to apply it to.
        try
        {
            await _db.Database.SqlQuery<DateTime>($"SELECT now() AT TIME ZONE {tz} AS \"Value\"").SingleAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidParameterValue)
        {
            return Problem(ControlStatsError.InvalidTimeZone);
        }

        var fromLocal = from?.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = to?.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var stats = _db.ControlStats.AsQueryable();
        if (fromLocal is not null) stats = stats.Where(x => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.Bucket, tz) >= fromLocal);
        if (toLocalExclusive is not null) stats = stats.Where(x => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.Bucket, tz) < toLocalExclusive);

        // Fine grained enough to derive every received view in memory, the row count is bounded by
        // days * shockers * controllers * types rather than by activity
        var received = await stats
            .Where(x => x.OwnerUserId == CurrentUser.Id)
            .GroupBy(x => new
            {
                Day = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.Bucket, tz).Date,
                x.ShockerId,
                x.ControllerUserId,
                x.Type,
                x.Live
            })
            .Select(g => new StatRow<ReceivedKey>(
                new ReceivedKey(g.Key.Day, g.Key.ShockerId, g.Key.ControllerUserId),
                g.Key.Type,
                g.Key.Live,
                g.Sum(x => x.Count),
                g.Sum(x => x.DurationMs),
                g.Sum(x => x.IntensityMs),
                g.Max(x => x.MaxIntensity)))
            .ToArrayAsync(cancellationToken);

        var given = await stats
            .Where(x => x.ControllerUserId == CurrentUser.Id)
            .GroupBy(x => new { x.OwnerUserId, x.Type, x.Live })
            .Select(g => new StatRow<GivenKey>(
                new GivenKey(g.Key.OwnerUserId),
                g.Key.Type,
                g.Key.Live,
                g.Sum(x => x.Count),
                g.Sum(x => x.DurationMs),
                g.Sum(x => x.IntensityMs),
                g.Max(x => x.MaxIntensity)))
            .ToArrayAsync(cancellationToken);

        var shockerIds = received.Select(x => x.Key.ShockerId).OfType<Guid>().Distinct().ToArray();
        var shockerNames = await _db.Shockers
            .Where(x => shockerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var userIds = received.Select(x => x.Key.ControllerUserId).OfType<Guid>()
            .Concat(given.Select(x => x.Key.OwnerUserId))
            .Distinct()
            .ToArray();
        var users = (await _db.Users
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name, x.Email })
                .ToArrayAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => new BasicUserInfo
            {
                Id = x.Id,
                Name = x.Name,
                Image = GravatarUtils.GetUserImageUrl(x.Email)
            });

        return Ok(new ControlStatsResponse
        {
            Received = new ReceivedControlStats
            {
                Totals = Totals(received),
                Shockers = received
                    .GroupBy(x => x.Key.ShockerId)
                    .Select(g => new ShockerControlStats
                    {
                        ShockerId = g.Key,
                        ShockerName = g.Key is { } id ? shockerNames.GetValueOrDefault(id) : null,
                        Stats = Totals(g)
                    })
                    .ToArray(),
                Controllers = received
                    .GroupBy(x => x.Key.ControllerUserId)
                    .Select(g => new UserControlStats
                    {
                        UserId = g.Key,
                        User = g.Key is { } id ? users.GetValueOrDefault(id) : null,
                        Stats = Totals(g)
                    })
                    .ToArray(),
                Days = received
                    .GroupBy(x => x.Key.Day)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyControlStats
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        Stats = Totals(g)
                    })
                    .ToArray()
            },
            Given = new GivenControlStats
            {
                Totals = Totals(given),
                Owners = given
                    .GroupBy(x => x.Key.OwnerUserId)
                    .Select(g => new UserControlStats
                    {
                        UserId = g.Key,
                        User = users.GetValueOrDefault(g.Key),
                        Stats = Totals(g)
                    })
                    .ToArray()
            }
        });
    }

    private static ControlStatTotals[] Totals<TKey>(IEnumerable<StatRow<TKey>> rows) => rows
        .GroupBy(x => (x.Type, x.Live))
        .OrderBy(g => g.Key.Type)
        .ThenBy(g => g.Key.Live)
        .Select(g =>
        {
            var durationMs = g.Sum(x => x.DurationMs);
            return new ControlStatTotals
            {
                Type = g.Key.Type,
                Live = g.Key.Live,
                Count = g.Sum(x => x.Count),
                DurationMs = durationMs,
                AverageIntensity = durationMs == 0 ? 0 : (double)g.Sum(x => x.IntensityMs) / durationMs,
                MaxIntensity = g.Max(x => x.MaxIntensity)
            };
        })
        .ToArray();

    private sealed record ReceivedKey(DateTime Day, Guid? ShockerId, Guid? ControllerUserId);

    private sealed record GivenKey(Guid OwnerUserId);

    private sealed record StatRow<TKey>(
        TKey Key,
        ControlType Type,
        bool Live,
        long Count,
        long DurationMs,
        long IntensityMs,
        byte MaxIntensity);
}
