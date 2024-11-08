using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<LoginSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IEnumerable<LoginSessionResponse>> ListSessions()
    {
        var sessions = await _sessionService.ListSessionsByUserId(CurrentUser.DbUser.Id);

        return sessions.Select(LoginSessionResponse.MapFrom);
    }
}