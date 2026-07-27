using System.Net;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Errors;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Problems;
using OpenShock.Common.Results;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Verifies a Cloudflare Turnstile response token for the current request.
    /// Returns <c>null</c> when verification succeeds, otherwise the <see cref="IActionResult"/> to return to the caller.
    /// </summary>
    private async Task<IActionResult?> VerifyTurnstileAsync(ICloudflareTurnstileService turnstileService, string turnstileResponse, CancellationToken cancellationToken)
    {
        var turnStile = await turnstileService.VerifyUserResponseTokenAsync(turnstileResponse, HttpContext.GetRemoteIP(), cancellationToken);
        if (turnStile is not CloudflareTurnstileError[] cfErrors) return null;

        if (cfErrors.All(err => err.IsClientError()))
            return Problem(TurnstileError.InvalidTurnstile);

        return Problem(new OpenShockProblem("InternalServerError", "Internal Server Error", HttpStatusCode.InternalServerError));
    }
}
