using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklist/usernames")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUsernameBlacklist([FromBody] AddUsernameBlacklistDto dto)
    {
        var entry = new UserNameBlacklist
        {
            Id = Guid.CreateVersion7(),
            Value = dto.Value,
            MatchType = dto.MatchType
        };
        _db.UserNameBlacklists.Add(entry);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
