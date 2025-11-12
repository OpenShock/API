using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [EndpointGroupName("v1")]
    [ProducesResponseType<LoginSessionResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IAsyncEnumerable<LoginSessionResponse> ListSessions()
    {
        return _sessionService.ListSessionsByUserIdAsync(CurrentUser.Id)
            .Select(LoginSessionResponse.MapFrom);
    }
}