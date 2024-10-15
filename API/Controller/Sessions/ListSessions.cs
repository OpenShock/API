using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [ProducesSuccess]
    public async Task<IActionResult> ListSessions()
    {
        var sessions = await _sessionService.ListSessions(CurrentUser.DbUser.Id);
        return RespondSuccess(sessions);
    }
}