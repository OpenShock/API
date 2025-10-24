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
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
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
    public async Task<IActionResult> GetShockerLogsV1([FromRoute] Guid shockerId, [FromQuery(Name="offset")] uint offset = 0,
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
    /// Get the logs for one or more shockers (cursor = createdAt strictly before "before")
    /// </summary>
    /// <param name="shockerIds">IDs of shockers to include (must all belong to the current user)</param>
    /// <param name="before">Return entries created strictly before this UTC timestamp. If omitted or default, now (UTC) is used.</param>
    /// <param name="limit">Max number of entries to return (1–500)</param>
    /// <response code="200">The logs</response>
    /// <response code="404">At least one requested shocker does not exist or is not owned by the current user</response>
    [HttpGet("logs")]
    [EnableRateLimiting("shocker-logs")]
    [ProducesResponseType<ShockerLogEntry[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> GetShockerLogsV2(
        [FromQuery] Guid[] shockerIds,
        [FromQuery(Name = "before")] DateTime before,
        [FromQuery, Range(1, 500)] uint limit = 100)
    {
        if (shockerIds.Length == 0)
            return Ok(Array.Empty<ShockerLogEntry>());

        // Validate ownership of all requested shockers
        var ownedIds = await _db.Shockers
            .Where(s => s.Device.OwnerId == CurrentUser.Id && shockerIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (ownedIds.Count != shockerIds.Length)
            return Problem(ShockerError.ShockerNotFound);

        // Normalize "before" to UTC; default to now if not provided
        DateTime beforeUtc = before == default
            ? DateTime.UtcNow
            : (before.Kind == DateTimeKind.Utc ? before : DateTime.SpecifyKind(before, DateTimeKind.Utc));

        var entries = await _db.ShockerControlLogs
            .Where(x => ownedIds.Contains(x.ShockerId) && x.CreatedAt < beforeUtc)
            .OrderByDescending(x => x.CreatedAt)
            .Take((int)limit)
            .Select(x => new ShockerLogEntry
            {
                Id = x.Id,
                ShockerId = x.ShockerId,
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
            .ToListAsync();

        return Ok(entries);
    }
}