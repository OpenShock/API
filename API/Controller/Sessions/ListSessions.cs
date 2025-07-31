using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    public async Task<IEnumerable<LoginSessionResponse>> ListSessions()
    {
        var sessions = await _sessionService.ListSessionsByUserIdAsync(CurrentUser.Id);

        return sessions.Select(LoginSessionResponse.MapFrom);
    }
}