using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType<LegacyDataResponse<IAsyncEnumerable<LogEntry>>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetShockerLogs([FromRoute] Guid shockerId, [FromQuery] uint offset = 0,
        [FromQuery] [Range(1, 500)] uint limit = 100)
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
}