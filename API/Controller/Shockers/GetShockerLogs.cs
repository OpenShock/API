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

    private static readonly IReadOnlyDictionary<string, SortFunc<ShockerControlLog>> AllLogsSorters =
        new Dictionary<string, SortFunc<ShockerControlLog>>(StringComparer.OrdinalIgnoreCase)
        {
            [AllLogsDefaultSort] = (q, desc) => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            ["intensity"] = (q, desc) => desc ? q.OrderByDescending(x => x.Intensity) : q.OrderBy(x => x.Intensity),
            ["duration"] = (q, desc) => desc ? q.OrderByDescending(x => x.Duration) : q.OrderBy(x => x.Duration),
            ["type"] = (q, desc) => desc ? q.OrderByDescending(x => x.Type) : q.OrderBy(x => x.Type),
            ["hubName"] = (q, desc) => desc ? q.OrderByDescending(x => x.Shocker.Device.Name) : q.OrderBy(x => x.Shocker.Device.Name),
            ["shockerName"] = (q, desc) => desc ? q.OrderByDescending(x => x.Shocker.Name) : q.OrderBy(x => x.Shocker.Name),
        };

    /// <summary>
    /// Get the logs for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <response code="200">The logs</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpGet("{shockerId}/logs")]
    [EnableRateLimiting("shocker-logs")]
    [ProducesResponseType<LegacyDataResponse<LogEntry[]>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetShockerLogs([FromRoute] Guid shockerId, [FromQuery(Name = "offset")] uint offset = 0,
        [FromQuery, Range(1, 500)] uint limit = 100)
    {
        var exists = await _db.Shockers.AnyAsync(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId);
        if (!exists) return Problem(ShockerError.ShockerNotFound);

        var logs = _db.ShockerControlLogs
            .Where(x => x.ShockerId == shockerId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((int)offset)
            .Take((int)limit)
            .Select(x => new LogEntry
            {
                Id = x.Id,
                Duration = x.Duration,
                Intensity = x.Intensity,
                Type = x.Type,
                CreatedOn = x.CreatedAt,
                ControlledBy = x.ControlledByUser == null
                    ? new ControlLogSenderLight
                    {
                        Id = Guid.Empty,
                        Name = "Guest",
                        Image = GravatarUtils.GuestImageUrl,
                        CustomName = x.CustomName
                    }
                    : new ControlLogSenderLight
                    {
                        Id = x.ControlledByUser.Id,
                        Name = x.ControlledByUser.Name,
                        Image = x.ControlledByUser.GetImageUrl(),
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
        var query = _db.ShockerControlLogs
            .Where(x => x.Shocker.Device.OwnerId == CurrentUser.Id);

        if (shockerIds is { Length: > 0 })
            query = query.Where(x => ((IEnumerable<Guid>)shockerIds).Contains(x.ShockerId));

        var ordered = query
            .ApplySort(pagination, AllLogsSorters, AllLogsDefaultSort)
            .ThenBy(x => x.Id);

        return await ordered.ToPagedResultAsync(
            x => new LogEntryWithHub
            {
                Id = x.Id,
                HubId = x.Shocker.Device.Id,
                HubName = x.Shocker.Device.Name,
                ShockerId = x.Shocker.Id,
                ShockerName = x.Shocker.Name,
                Duration = x.Duration,
                Intensity = x.Intensity,
                Type = x.Type,
                CreatedOn = x.CreatedAt,
                ControlledBy = x.ControlledByUser == null
                    ? new ControlLogSenderLight
                    {
                        Id = Guid.Empty,
                        Name = "Guest",
                        Image = GravatarUtils.GuestImageUrl,
                        CustomName = x.CustomName
                    }
                    : new ControlLogSenderLight
                    {
                        Id = x.ControlledByUser.Id,
                        Name = x.ControlledByUser.Name,
                        Image = x.ControlledByUser.GetImageUrl(),
                        CustomName = x.CustomName
                    }
            },
            pagination,
            cancellationToken);
    }
}