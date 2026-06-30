using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset. Retired: use POST /2/account/password-reset instead.
    /// </summary>
    [HttpPost("reset")]
    [Obsolete("Retired. Use POST /2/account/password-reset instead.")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [MapToApiVersion("1")]
    public IActionResult PasswordResetInitiate() => Problem(GoneError.EndpointRetired("POST /2/account/password-reset"));
}
