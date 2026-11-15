using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Redis;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [ApiVersion(1)]
    public LoginSessionResponse GetSelfSession()
    {
        return LoginSessionResponse.MapFrom(GetRequiredItem<LoginSession>(), _logger);
    }
}
