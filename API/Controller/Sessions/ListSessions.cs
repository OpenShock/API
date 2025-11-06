using System.Net.Mime;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    [HttpGet]
    [ProducesResponseType<LoginSessionResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IAsyncEnumerable<LoginSessionResponse> ListSessions([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        return _sessionService.ListSessionsByUserIdAsync(CurrentUser.Id)
            .WithCancellation(cancellationToken)
            .Select(LoginSessionResponse.MapFrom);
    }
}