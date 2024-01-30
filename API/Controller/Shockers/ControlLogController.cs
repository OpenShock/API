using System.ComponentModel.DataAnnotations;
using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Utils;

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
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<IEnumerable<LogEntry>>> GetShockerLogs([FromRoute] Guid shockerId, [FromQuery] uint offset = 0,
        [FromQuery] [Range(1, 500)] uint limit = 100)
    {
        var exists = await _db.Shockers.AnyAsync(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == shockerId);
        if (!exists) return EBaseResponse<IEnumerable<LogEntry>>("Shocker does not exist", HttpStatusCode.NotFound);

        var logs = await _db.ShockerControlLogs.Where(x => x.ShockerId == shockerId)
            .OrderByDescending(x => x.CreatedOn).Skip((int)offset).Take((int)limit).Select(x => new LogEntry
            {
                Id = x.Id,
                Duration = x.Duration,
                Intensity = x.Intensity,
                Type = x.Type,
                CreatedOn = x.CreatedOn,
                ControlledBy = x.ControlledByNavigation == null
                    ? new ControlLogSenderLight
                    {
                        Id = Guid.Empty,
                        Name = "Guest",
                        Image = new Uri("https://www.gravatar.com/avatar/0?d=https%3A%2F%2Fshocklink.net%2Fstatic%2Fimages%2FIcon512.png"),
                        CustomName = x.CustomName
                    }
                    : new ControlLogSenderLight
                    {
                        Id = x.ControlledByNavigation.Id,
                        Name = x.ControlledByNavigation.Name,
                        Image = GravatarUtils.GetImageUrl(x.ControlledByNavigation.Email),
                        CustomName = x.CustomName
                    }
            }).ToListAsync();

        return new BaseResponse<IEnumerable<LogEntry>>
        {
            Data = logs
        };
    }
}