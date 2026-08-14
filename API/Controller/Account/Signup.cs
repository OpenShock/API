using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user. Retired: use POST /2/account/signup instead.
    /// </summary>
    [HttpPost("signup")]
    [Obsolete("Retired. Use POST /2/account/signup instead.")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [MapToApiVersion("1")]
    public IActionResult SignUp() => Problem(GoneError.EndpointRetired("POST /2/account/signup"));
}
