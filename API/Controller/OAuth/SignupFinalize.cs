using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.OAuth;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Finalize an OAuth flow by creating a new account with the external identity.
    /// </summary>
    /// <remarks>
    /// Authenticates via the temporary OAuth flow cookie (set during the provider callback).
    /// Sets the regular session cookie on success. No access/refresh tokens are returned.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="body">Request body containing optional <c>Email</c> and <c>Username</c> overrides.</param>
    /// <param name="connectionService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Account created, external identity linked, and client authenticated.</response>
    /// <response code="400">Flow not found, missing data, or invalid username.</response>
    /// <response code="409">External already linked or username/email already exists.</response>
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/signup-finalize")]
    [ProducesResponseType(typeof(LoginV2OkResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status409Conflict, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthSignupFinalize(
        [FromRoute] string provider,
        [FromBody] OAuthFinalizeRequest body,
        [FromServices] IOAuthConnectionService connectionService,
        CancellationToken cancellationToken)
    {
        // If domain is not supported for cookies, cancel the flow
        var domain = GetCurrentCookieDomain();
        if (string.IsNullOrEmpty(domain))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.InternalError);
        }
        
        var result = await ValidateOAuthFlowAsync();
        if (!result.TryPickT0(out var auth, out var error))
        {
            return error switch
            {
                OAuthValidationError.FlowStateMissing => Problem(OAuthError.FlowNotFound),
                _ => Problem(OAuthError.InternalError)
            };
        }
        
        if (User.HasOpenShockUserIdentity())
        {
            return Problem(OAuthError.AnonymousOnlyEndpoint);
        }

        // 1) Defense-in-depth: ensure the flow’s provider matches the route
        if (!string.Equals(auth.Provider, provider, StringComparison.OrdinalIgnoreCase) || auth.Flow != OAuthFlow.LoginOrCreate)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMismatch);
        }

        // External identity basics from claims (added by your handler)
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var externalAccountName = auth.Principal.FindFirst(ClaimTypes.Name)?.Value;
        var externalAccountEmail = auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = body.Username ?? externalAccountName;
        var email = body.Email ?? externalAccountEmail;

        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName))
        {
            return Problem(OAuthError.FlowMissingData);
        }

        var isVerifiedString = auth.Principal.FindFirst(OAuthConstants.ClaimEmailVerified)?.Value;
        var isEmailTrusted = IsTruthy(isVerifiedString) && string.Equals(externalAccountEmail, email, StringComparison.InvariantCultureIgnoreCase);

        // Do not allow creation if this external is already linked anywhere.
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, externalId, cancellationToken);
        if (existing is not null)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ExternalAlreadyLinked);
        }

        var created = await _accountService.CreateOAuthOnlyAccountAsync(
            email,
            displayName,
            provider,
            externalId,
            externalAccountName,
            isEmailTrusted
        );

        if (!created.TryPickT0(out var newUser, out _))
        {
            // Username or email already exists — conflict.
            // Do NOT clear the flow cookie so the frontend can retry with a different username.
            return Problem(SignupError.UsernameOrEmailExists);
        }

        // Authenticate the client if its activated (create session and set session cookie)
        if (newUser.Value.ActivatedAt is not null)
        {
            await CreateSession(newUser.Value.Id, domain);
        }

        // Clear the temporary OAuth flow cookie.
        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        return Ok(LoginV2OkResponse.FromUser(newUser.Value));

        static bool IsTruthy(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.Trim().ToLowerInvariant() switch
            {
                "0" or "no" or "false" => false,
                "1" or "yes" or "true" => true,
                _ => false
            };
        }
    }
}
