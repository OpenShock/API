using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common;
using ShockLink.Common.Models;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    public static readonly Uri DefaultAvatarUri = ImagesApi.GetImageRoot(Constants.DefaultAvatar);

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
                        Image = DefaultAvatarUri,
                        CustomName = x.CustomName
                    }
                    : new ControlLogSenderLight
                    {
                        Id = x.ControlledByNavigation.Id,
                        Name = x.ControlledByNavigation.Name,
                        Image = ImagesApi.GetImageRoot(x.ControlledByNavigation.Image),
                        CustomName = x.CustomName
                    }
            }).ToListAsync();

        return new BaseResponse<IEnumerable<LogEntry>>
        {
            Data = logs
        };
    }
}