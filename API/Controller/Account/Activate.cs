using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Account;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Internal.Common.Problems;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Activate account
    /// </summary>
    /// <remarks>
    /// In the browser that signed up, the emailed token is enough. From anywhere else the caller must also
    /// show the account is theirs, since the token only proves control of the inbox and anyone can sign up
    /// with somebody else's address: send the account's password in the body, or, for an account created
    /// through OAuth, start a sign-in with the linked provider passing this token as <c>activationToken</c>.
    /// </remarks>
    /// <response code="200">Account activated</response>
    /// <response code="400">No pending activation matches the token</response>
    /// <response code="403">The token is valid, but the account's password or linked sign-in is also required</response>
    [HttpPost("activate")]
    [ApiVersion(1)]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // AccountActivationNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // AccountActivationCredentialRequired
    public async Task<IActionResult> Activate(
        [FromQuery(Name = "token")] string token,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ActivateRequest? body,
        CancellationToken cancellationToken)
    {
        SignupBrowserCookie.TryGetUserId(HttpContext, out var signupBrowserUserId);

        var result = await _accountService.ActivateAccountAsync(token, signupBrowserUserId, body?.Password, cancellationToken);

        return result switch
        {
            Results.Success => Ok(),
            ActivationCredentialRequired required => Problem(AccountError.AccountActivationCredentialRequired(required.HasPassword, required.OAuthProviders, required.PasswordInvalid)),
            Results.NotFound => Problem(AccountError.AccountActivationNotFound)
        };
    }

    public sealed class ActivateRequest
    {
        /// <summary>The account's password. Only needed when activating from a browser other than the one that signed up.</summary>
        public string? Password { get; init; }
    }
}
