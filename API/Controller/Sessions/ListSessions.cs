using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [ProducesSlimSuccess<IEnumerable<LoginSessionResponse>>]
    public async Task<IEnumerable<LoginSessionResponse>> ListSessions()
    {
        
        return await _sessionService.ListSessions(CurrentUser.DbUser.Id);
    }
}