using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    public async Task<IEnumerable<LoginSessionResponse>> ListSessions()
    {
        var sessions = await _sessionService.ListSessionsByUserId(CurrentUser.Id);

        return sessions.Select(LoginSessionResponse.MapFrom);
    }
}