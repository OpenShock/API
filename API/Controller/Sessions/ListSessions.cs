using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

using OpenShock.Common.ApiVersioning;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [ProducesResponseType<LoginSessionResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ApiVersion(1)]
    public IAsyncEnumerable<LoginSessionResponse> ListSessions()
    {
        return _sessionService.ListSessionsByUserIdAsync(CurrentUser.Id)
            .Select(LoginSessionResponse.MapFrom);
    }
}