using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset. Retired: use POST /2/account/password-reset instead.
    /// </summary>
    [Obsolete("Retired. Use POST /2/account/password-reset instead.")]
    [HttpPost("reset")]
    [ApiVersion(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult PasswordResetInitiate() => Problem(GoneError.EndpointRetired("POST /2/account/password-reset"));
}
