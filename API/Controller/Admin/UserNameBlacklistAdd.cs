using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklist/usernames")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUsernameBlacklist([FromBody] AddUsernameBlacklistDto body)
    {
        var entry = new UserNameBlacklist
        {
            Id = Guid.CreateVersion7(),
            Value = body.Value,
            MatchType = body.MatchType
        };
        _db.UserNameBlacklists.Add(entry);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
