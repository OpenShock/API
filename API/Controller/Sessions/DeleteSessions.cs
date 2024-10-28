using System.Net;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpDelete("{sessionId}")]
    [ProducesSlimSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "SessionNotFound")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        var loginSession = await _sessionService.GetSession(sessionId);

        // If the session was not found, or does not belong to the current user (unless its an admin) return NotFound
        if (loginSession == null || (loginSession.UserId != CurrentUser.DbUser.Id && CurrentUser.DbUser.Rank < Common.Models.RankType.Admin)) 
        {
            return Problem(SessionError.SessionNotFound);
        }

        await _sessionService.DeleteSession(loginSession);

        return RespondSlimSuccess();
    }
}