using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpGet("{id:guid}/logs")]
    public async Task<BaseResponse<IEnumerable<LogEntry>>> GetShockerLogs(Guid id, [FromQuery] uint offset = 0,
        [FromQuery] [Range(1, 500)] uint limit = 100)
    {
        var exists = await _db.Shockers.AnyAsync(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists) return EBaseResponse<IEnumerable<LogEntry>>("Shocker does not exist", HttpStatusCode.NotFound);

        var logs = await _db.ShockerControlLogs.Where(x => x.ShockerId == id)
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