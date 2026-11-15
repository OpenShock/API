using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Authenticate a user. Retired: use POST /2/account/login instead.
    /// </summary>
    [Obsolete("Retired. Use POST /2/account/login instead.")]
    [HttpPost("login")]
    [ApiVersion(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Login() => Problem(GoneError.EndpointRetired("POST /2/account/login"));
}
