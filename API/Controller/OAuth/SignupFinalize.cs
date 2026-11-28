using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.OAuth;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
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
    /// <param name="accountOptions"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("{provider}/signup-finalize")]
    [ApiVersion(1)]
    [EnableRateLimiting("auth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthSignupFinalize(
        [FromRoute] string provider,
        [FromBody] OAuthFinalizeRequest body,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] AccountOptions accountOptions,
        CancellationToken cancellationToken)
    {
        if (!accountOptions.RegistrationEnabled)
            return Problem(SignupError.RegistrationDisabled);

        // If domain is not supported for cookies, cancel the flow
        var domain = GetCurrentCookieDomain();
        if (string.IsNullOrEmpty(domain))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.InternalError);
        }
        
        var result = await ValidateOAuthFlowAsync();
        if (result is not ValidatedFlowContext auth)
        {
            var error = (OAuthValidationError)result.Value!;
            return error switch
            {
                OAuthValidationError.FlowStateMissing => Problem(OAuthError.FlowNotFound),
                OAuthValidationError.FlowDataMissingOrInvalid => Problem(OAuthError.InternalError),
                _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
            };
        }
        
        if (User.HasOpenShockUserIdentity())
        {
            return Problem(OAuthError.FlowRequiresAnonymous);
        }

        // 1) Defense-in-depth: ensure the flow’s provider matches the route
        if (!string.Equals(auth.Provider, provider, StringComparison.OrdinalIgnoreCase) || auth.Flow != OAuthFlow.LoginOrCreate)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMismatch);
        }

        // External identity basics from claims (added by your handler)
        var externalAccountEmail = auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var username = body.Username ?? auth.ExternalAccountDisplayName ?? auth.ExternalAccountName;
        var email = body.Email ?? externalAccountEmail;

        if (string.IsNullOrWhiteSpace(auth.ExternalAccountId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username))
        {
            return Problem(OAuthError.FlowMissingData);
        }

        var isVerifiedString = auth.Principal.FindFirst(OAuthConstants.ClaimEmailVerified)?.Value;
        var isEmailTrusted = StringUtils.IsTruthy(isVerifiedString) && string.Equals(externalAccountEmail, email, StringComparison.InvariantCultureIgnoreCase);

        // Do not allow creation if this external is already linked to a real account.
        Guid? replacePendingUserId = null;
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, auth.ExternalAccountId, cancellationToken);
        if (existing is not null)
        {
            if (existing.User.ActivatedAt is not null || existing.User.UserDeactivation is not null)
            {
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                return Problem(OAuthError.ExternalAlreadyLinked);
            }

            // Linked to a signup this identity started but never activated, most likely because the email
            // was mistyped and the link never arrived. The caller just proved they hold the identity, and a
            // pending account has nothing in it, so let them start over instead of being stuck with it.
            if (isEmailTrusted)
            {
                // The provider vouches for the new address: replace the pending account with a real one below.
                // It is only removed if that succeeds, so a taken username leaves the signup to retry with.
                replacePendingUserId = existing.UserId;
            }
            else
            {
                var restarted = await _accountService.RestartPendingSignupAsync(existing.UserId, email, username, cancellationToken);
                User pendingUser;
                switch (restarted.Value)
                {
                    case User restartedUser:
                        pendingUser = restartedUser;
                        break;
                    case AccountWithEmailOrUsernameExists:
                        // Keep the flow cookie so the frontend can retry with other details.
                        return Problem(SignupError.UsernameOrEmailExists);
                    case TooManyActivationResends:
                        // Nothing was changed and the link already mailed still works. Other details would
                        // be refused the same way until the window rolls over, so the flow ends here.
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return Problem(AccountError.AccountActivationResendTooMany);
                    default:
                        // No longer a pending signup: it was activated, deactivated or cleaned up since the
                        // lookup above. Nothing is wrong with the details, so do not ask for different
                        // ones: signing in with the provider again takes the path that fits what it is now.
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return Problem(OAuthError.FlowNotFound);
                }

                ActivationResendCookie.Set(HttpContext, pendingUser.Id, domain);
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

                return Ok(LoginV2OkResponse.FromUser(pendingUser));
            }
        }

        var created = await _accountService.CreateOAuthOnlyAccountAsync(
            email,
            username,
            provider,
            auth.ExternalAccountId,
            auth.ExternalAccountDisplayName ?? auth.ExternalAccountName,
            isEmailTrusted,
            replacePendingUserId,
            cancellationToken
        );

        if (created is not User newUser)
        {
            // Username or email already exists — conflict.
            // Do NOT clear the flow cookie so the frontend can retry with a different username.
            return Problem(SignupError.UsernameOrEmailExists);
        }

        // Authenticate the client if its activated (create session and set session cookie)
        if (newUser.ActivatedAt is not null)
        {
            await CreateSession(newUser.Id, domain);
        }
        else
        {
            // Lets this browser ask for the activation email again without typing the address.
            ActivationResendCookie.Set(HttpContext, newUser.Id, domain);
        }

        // Clear the temporary OAuth flow cookie.
        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        return Ok(LoginV2OkResponse.FromUser(newUser));
    }
}
