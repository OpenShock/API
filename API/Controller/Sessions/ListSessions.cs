using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    public IAsyncEnumerable<LoginSessionResponse> ListSessions()
    {
        return _sessionService.ListSessionsByUserIdAsync(CurrentUser.Id)
            .Select(LoginSessionResponse.MapFrom);
    }
}