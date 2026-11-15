using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user. Retired: use POST /2/account/signup instead.
    /// </summary>
    [Obsolete("Retired. Use POST /2/account/signup instead.")]
    [HttpPost("signup")]
    [ApiVersion(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult SignUp() => Problem(GoneError.EndpointRetired("POST /2/account/signup"));
}
