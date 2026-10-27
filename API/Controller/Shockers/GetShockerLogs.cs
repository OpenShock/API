using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using OpenShock.Common.Utils.Pagination;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    private const string AllLogsDefaultSort = "createdOn";

    private static readonly IReadOnlyDictionary<string, SortFunc<LogRow>> AllLogsSorters =
        new Dictionary<string, SortFunc<LogRow>>(StringComparer.OrdinalIgnoreCase)
        {
            [AllLogsDefaultSort] = (q, desc) => desc ? q.OrderByDescending(x => x.Log.CreatedAt) : q.OrderBy(x => x.Log.CreatedAt),
            ["intensity"] = (q, desc) => desc ? q.OrderByDescending(x => x.Log.Intensity) : q.OrderBy(x => x.Log.Intensity),
            ["duration"] = (q, desc) => desc ? q.OrderByDescending(x => x.Log.Duration) : q.OrderBy(x => x.Log.Duration),
            ["type"] = (q, desc) => desc ? q.OrderByDescending(x => x.Log.Type) : q.OrderBy(x => x.Log.Type),
            ["hubName"] = (q, desc) => desc ? q.OrderByDescending(x => x.Shocker.Device.Name) : q.OrderBy(x => x.Shocker.Device.Name),
            ["shockerName"] = (q, desc) => desc ? q.OrderByDescending(x => x.Shocker.Name) : q.OrderBy(x => x.Shocker.Name),
        };

    private sealed class LogRow
    {
        public required ShockerControlLog Log { get; init; }
        public required Shocker Shocker { get; init; }
        public required User? Controller { get; init; }
    }

    /// <summary>
    /// Get the logs for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">The logs</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpGet("{shockerId}/logs")]
    [EnableRateLimiting("shocker-logs")]
    [ProducesResponseType<LegacyDataResponse<LogEntry[]>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetShockerLogs([FromRoute] Guid shockerId, [FromQuery(Name = "offset")] uint offset = 0,
        [FromQuery, Range(1, 500)] uint limit = 100,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Shockers.AnyAsync(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId, cancellationToken);
        if (!exists) return Problem(ShockerError.ShockerNotFound);

        var page = _db.ShockerControlLogs
            .Where(x => x.ShockerId == shockerId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((int)offset)
            .Take((int)limit);

        var logs = (from x in page
                join u in _db.Users on x.ControlledByUserId equals (Guid?)u.Id into controllers
                from u in controllers.DefaultIfEmpty()
                orderby x.CreatedAt descending
                select new LogEntry
                {
                    Id = x.Id,
                    Duration = x.Duration,
                    Intensity = x.Intensity,
                    Type = x.Type,
                    CreatedOn = x.CreatedAt,
                    ControlledBy = u != null
                        ? new ControlLogSenderLight
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Image = u.GetImageUrl(),
                            CustomName = x.CustomName
                        }
                        : x.ControlledByUserId == null
                            ? new ControlLogSenderLight
                            {
                                Id = Guid.Empty,
                                Name = "Guest",
                                Image = GravatarUtils.GuestImageUrl,
                                CustomName = x.CustomName
                            }
                            : new ControlLogSenderLight
                            {
                                Id = x.ControlledByUserId.Value,
                                Name = "Deleted User",
                                Image = GravatarUtils.GuestImageUrl,
                                CustomName = x.CustomName
                            }
                })
            .AsAsyncEnumerable();

        return LegacyDataOk(logs);
    }


    /// <summary>
    /// Get a paged set of control logs across the caller's shockers.
    /// </summary>
    /// <param name="pagination">Page, sort, and search parameters. Supported sort keys: createdOn (default), intensity, duration, type, hubName, shockerName.</param>
    /// <param name="shockerIds">Optional shocker ID filter. When omitted or empty, logs for all of the caller's shockers are returned.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">A page of logs.</response>
    [HttpGet("logs")]
    [EnableRateLimiting("shocker-logs")]
    [ProducesResponseType<PagedResult<LogEntryWithHub>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [MapToApiVersion("1")]
    public async Task<PagedResult<LogEntryWithHub>> GetAllShockerLogs(
        [FromQuery] PaginationQuery pagination,
        [FromQuery(Name = "shockerIds")] Guid[]? shockerIds,
        CancellationToken cancellationToken)
    {
        var logs = _db.ShockerControlLogs.Where(x => x.OwnerUserId == CurrentUser.Id);

        if (shockerIds is { Length: > 0 })
            logs = logs.Where(x => ((IEnumerable<Guid>)shockerIds).Contains(x.ShockerId));

        var query = from x in logs
            join s in _db.Shockers on x.ShockerId equals s.Id
            join u in _db.Users on x.ControlledByUserId equals (Guid?)u.Id into controllers
            from u in controllers.DefaultIfEmpty()
            select new LogRow { Log = x, Shocker = s, Controller = u };

        var ordered = query
            .ApplySort(pagination, AllLogsSorters, AllLogsDefaultSort)
            .ThenBy(x => x.Log.Id);

        return await ordered.ToPagedResultAsync(
            x => new LogEntryWithHub
            {
                Id = x.Log.Id,
                HubId = x.Shocker.Device.Id,
                HubName = x.Shocker.Device.Name,
                ShockerId = x.Shocker.Id,
                ShockerName = x.Shocker.Name,
                Duration = x.Log.Duration,
                Intensity = x.Log.Intensity,
                Type = x.Log.Type,
                CreatedOn = x.Log.CreatedAt,
                ControlledBy = x.Controller != null
                    ? new ControlLogSenderLight
                    {
                        Id = x.Controller.Id,
                        Name = x.Controller.Name,
                        Image = x.Controller.GetImageUrl(),
                        CustomName = x.Log.CustomName
                    }
                    : x.Log.ControlledByUserId == null
                        ? new ControlLogSenderLight
                        {
                            Id = Guid.Empty,
                            Name = "Guest",
                            Image = GravatarUtils.GuestImageUrl,
                            CustomName = x.Log.CustomName
                        }
                        : new ControlLogSenderLight
                        {
                            Id = x.Log.ControlledByUserId.Value,
                            Name = "Deleted User",
                            Image = GravatarUtils.GuestImageUrl,
                            CustomName = x.Log.CustomName
                        }
            },
            pagination,
            cancellationToken);
    }
}