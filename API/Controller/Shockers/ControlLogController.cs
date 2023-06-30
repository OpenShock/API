using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class ControlLogController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public ControlLogController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<BaseResponse<IEnumerable<LogEntry>>> GetShocker(Guid id, [FromQuery] uint offset = 0,
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
                ControlledBy = new GenericIni
                {
                    Id = x.ControlledByNavigation.Id,
                    Name = x.ControlledByNavigation.Name,
                    Image = ImagesApi.GetImageRoot(x.ControlledByNavigation.Image)
                }
            }).ToListAsync();

        return new BaseResponse<IEnumerable<LogEntry>>
        {
            Data = logs
        };
    }

    public class LogEntry
    {
        public required Guid Id { get; set; }

        public required DateTime CreatedOn { get; set; }

        public required ControlType Type { get; set; }

        public required GenericIni ControlledBy { get; set; }

        public required byte Intensity { get; set; }

        public required uint Duration { get; set; }
    }
}