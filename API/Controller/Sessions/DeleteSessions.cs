﻿using System.Net;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpDelete("{sessionId}")]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "SessionNotFound")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        var result = await _sessionService.DeleteSession(CurrentUser.DbUser.Id, sessionId);

        return result.Match(
            success => RespondSuccessSimple("Successfully deleted session"),
            error => Problem(SessionError.SessionNotFound));
    }
}