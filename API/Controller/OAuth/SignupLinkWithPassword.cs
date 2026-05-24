using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.OAuth;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Link an in-flight OAuth signup to an existing OpenShock account by authenticating with the
    /// account's password. Used when <c>POST /{provider}/signup-finalize</c> returned
    /// <c>Account.Email.TakenLinkAvailable</c>: the user holds a valid OAuth flow cookie, the
    /// provider-supplied email matches an existing account that has a password, and the user proves
    /// ownership of that account here.
    /// </summary>
    /// <remarks>
    /// Authenticates via the temporary OAuth flow cookie (anonymous user) and the account password.
    /// On success the OAuth connection is attached to the existing account, a session is created,
    /// and the flow cookie is cleared.
    /// </remarks>
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/signup-link-password")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LoginV2OkResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthSignupLinkWithPassword(
        [FromRoute] string provider,
        [FromBody] OAuthLinkPasswordRequest body,
        [FromServices] IOAuthConnectionService connectionService,
        CancellationToken cancellationToken)
    {
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
            return Problem(OAuthError.FlowRequiresAnonymous);
        }

        // Defense-in-depth: ensure the flow's provider matches the route and the flow type is right.
        if (!string.Equals(auth.Provider, provider, StringComparison.OrdinalIgnoreCase) || auth.Flow != OAuthFlow.LoginOrCreate)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMismatch);
        }

        // The provider-supplied email is the only email we trust here — the client does not pass
        // an email of their own. This prevents the endpoint from being used to probe arbitrary
        // addresses for password validity.
        var providerEmail = auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(auth.ExternalAccountId) || string.IsNullOrWhiteSpace(providerEmail))
        {
            return Problem(OAuthError.FlowMissingData);
        }

        // No Turnstile here: the flow cookie already proves a completed OAuth round-trip with the
        // provider, which is stronger bot-gating than Turnstile. Password brute-force is bounded
        // by the shared `auth` rate-limit bucket.

        // Reject if the external identity got linked by a parallel flow between the conflict on
        // signup-finalize and now.
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, auth.ExternalAccountId, cancellationToken);
        if (existing is not null)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ExternalAlreadyLinked);
        }

        // Authenticate using the provider's email as the lookup key. Map failures to the same
        // problems LoginV2 returns so error shape & timing don't differ from a normal login attempt.
        var getAccountResult = await _accountService.GetAccountByCredentialsAsync(providerEmail, body.Password, cancellationToken);
        if (!getAccountResult.TryPickT0(out var account, out var loginErrors))
        {
            return loginErrors.Match(
                notFound => Problem(LoginError.InvalidCredentials),
                deactivated => Problem(AccountError.AccountDeactivated),
                notActivated => Problem(AccountError.AccountNotActivated),
                oauthOnly => Problem(AccountError.AccountOAuthOnly)
            );
        }

        // One connection per (user, provider). If this account already has this provider attached,
        // surface a distinct error so the frontend can route the user to settings/connections.
        if (await connectionService.HasConnectionAsync(account.Id, provider, cancellationToken))
        {
            return Problem(OAuthError.ProviderAlreadyLinkedToAccount);
        }

        var linked = await connectionService.TryAddConnectionAsync(
            account.Id,
            provider,
            auth.ExternalAccountId,
            auth.ExternalAccountDisplayName ?? auth.ExternalAccountName,
            cancellationToken);
        if (!linked)
        {
            // Race: either the external id or the (user, provider) pair was claimed in parallel.
            return Problem(OAuthError.LinkFailed);
        }

        _logger.LogInformation(
            "Linked OAuth provider {Provider} (external id {ExternalId}) to existing user {UserId} via password",
            provider, auth.ExternalAccountId, account.Id);

        await CreateSession(account.Id, domain);
        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        return Ok(LoginV2OkResponse.FromUser(account));
    }
}
