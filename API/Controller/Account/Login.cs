using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Authenticate a user. Retired: use POST /2/account/login instead.
    /// </summary>
    [HttpPost("login")]
    [Obsolete("Retired. Use POST /2/account/login instead.")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [MapToApiVersion("1")]
    public IActionResult Login() => Problem(GoneError.EndpointRetired("POST /2/account/login"));
}
