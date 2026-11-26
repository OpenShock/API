using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.OAuth;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Extensions;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Handoff after provider callback. Decides next step (create, link, or direct sign-in).
    /// </summary>
    /// <remarks>
    /// Reads the temp OAuth flow principal (flow cookie set by middleware).
    /// If a matching connection exists -> signs in and redirects home.
    /// Otherwise -> redirects frontend to continue the chosen flow.
    /// </remarks>
    [HttpGet("{provider}/handoff")]
    [ApiVersion(1)]
    [EnableRateLimiting("auth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] FrontendOptions frontendOptions,
        CancellationToken cancellationToken)
    {
        var result = await ValidateOAuthFlowAsync();
        if (result is not ValidatedFlowContext auth)
        {
            var error = (OAuthValidationError)result.Value!;
            return error switch
            {
                OAuthValidationError.FlowStateMissing => RedirectFrontendError("oauthFlowNotStarted"),
                OAuthValidationError.FlowDataMissingOrInvalid => RedirectFrontendError("internalError"),
                _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
            };
        }

        // 1) Defense-in-depth: ensure the flow’s provider matches the route
        if (!string.Equals(auth.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return RedirectFrontendError("providerMismatch");
        }

        var connection = await connectionService
            .GetByProviderExternalIdAsync(provider, auth.ExternalAccountId, cancellationToken);

        switch (auth.Flow)
        {
            case OAuthFlow.LoginOrCreate:
            {
                if (User.HasOpenShockUserIdentity())
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("mustBeAnonymous");
                }

                if (connection is null)
                {
                    // If the provider returned an email that already belongs to an existing OpenShock
                    // user, do not allow creating a second account with the same email. The user must
                    // log in to that account and link the provider from the connections settings.
                    // Only for an address the provider verified: an unverified one is whatever the caller typed
                    // at the provider, and answering for it would tell anyone which emails are registered.
                    var externalEmail = auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrWhiteSpace(externalEmail) &&
                        StringUtils.IsTruthy(auth.Principal.FindFirst(OAuthConstants.ClaimEmailVerified)?.Value) &&
                        await _accountService.IsEmailRegisteredAsync(externalEmail, cancellationToken))
                    {
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return RedirectFrontendError("emailAlreadyRegistered");
                    }

                    // No connection -> continue to CREATE flow on frontend
                    return RedirectFrontendPath($"/oauth/{Uri.EscapeDataString(provider)}/create");
                }

                // Session auth rejects (and deletes) sessions of unactivated or deactivated users, so creating one
                // here would bounce the user silently back to the login page. Surface the reason instead.
                if (connection.User.UserDeactivation is not null)
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("accountDeactivated");
                }

                // A flow started from the account's emailed activation link that comes back with the linked
                // identity shows inbox and credential are held by the same party, so the account is theirs:
                // activate it and carry on to the sign-in below.
                var isActivated = connection.User.ActivatedAt is not null;
                if (!isActivated && auth.Properties.ActivationToken is { } activationToken)
                {
                    isActivated = await _accountService.TryActivateAccountAsync(connection.UserId, activationToken, cancellationToken);
                }

                if (!isActivated)
                {
                    // The provider vouched for the identity, so the caller holds the account's credential: (re)send
                    // the activation link (rate limited inside the service). Opening it is still required, the
                    // address on the account was typed in at signup and is not known to be theirs.
                    try
                    {
                        await _accountService.ResendActivationEmailAsync(connection.UserId, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Best effort: the redirect below still tells the user what is going on.
                        _logger.LogError(ex, "Failed to resend activation email on OAuth login");
                    }

                    // Back to the signup form with the flow cookie kept: it explains that activation is
                    // pending, and lets someone who mistyped their email correct it instead of being stuck
                    // with an account they can never activate (see OAuthSignupFinalize).
                    return RedirectFrontendPath($"/oauth/{Uri.EscapeDataString(provider)}/create?pending=1");
                }

                // Direct sign-in
                var domain = GetCurrentCookieDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("internalError");
                }

                await CreateSession(connection.UserId, domain);

                // Flow cookie no longer needed
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

                return RedirectFrontendPath("/home");
            }

            case OAuthFlow.Link:
            {
                var identity = User.TryGetOpenShockUserIdentity();
                if (identity is null)
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("mustBeAuthenticated");
                }

                var userId = identity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier);

                if (connection is not null)
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendConnections(connection.UserId == userId ? "alreadyLinked" : "linkedToAnotherAccount");
                } 
                
                var ok = await connectionService.TryAddConnectionAsync(userId, provider, auth.ExternalAccountId, auth.ExternalAccountDisplayName ?? auth.ExternalAccountName, actorId: userId, cancellationToken);
                if (!ok)
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendConnections("linkFailed");
                }

                // Direct sign-in
                var domain = GetCurrentCookieDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendConnections("internalError");
                }

                await CreateSession(userId, domain);

                // Flow cookie no longer needed
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

                return RedirectFrontendConnections("linked");
            }

            default:
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                return RedirectFrontendError("internalError");
        }

        // --- helpers ---

        RedirectResult RedirectFrontendPath(string path)
        {
            return Redirect(new Uri(frontendOptions.BaseUrl, path).ToString());
        }

        RedirectResult RedirectFrontendError(string errorType)
            => RedirectFrontendPath($"/oauth/error?error={Uri.EscapeDataString(errorType)}");
        
        RedirectResult RedirectFrontendConnections(string statusType)
            => RedirectFrontendPath($"/settings/connections?status={Uri.EscapeDataString(statusType)}");
    }
}
