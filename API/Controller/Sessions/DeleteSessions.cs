using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using System.Net.Mime;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // SessionNotFound
    public async Task<IActionResult> DeleteSession([FromRoute] Guid sessionId)
    {
        var loginSession = await _sessionService.GetSessionByIdAsync(sessionId);

        // If the session was not found, or the user does not have the privledges to access it, return NotFound
        if (loginSession is null || !CurrentUser.IsUserOrRole(loginSession.UserId, RoleType.Admin))
        {
            return Problem(SessionError.SessionNotFound);
        }

        await _sessionService.DeleteSessionAsync(loginSession);

        return Ok();
    }
}