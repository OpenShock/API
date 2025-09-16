using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Options;
using OpenShock.API.OAuth;
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
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] FrontendOptions frontendOptions,
        CancellationToken cancellationToken)
    {
        var result = await ValidateOAuthFlowAsync();
        if (!result.TryPickT0(out var auth, out var error))
        {
            return error switch
            {
                OAuthValidationError.FlowStateMissing => RedirectFrontendError("OAuthFlowNotStarted"),
                _ => RedirectFrontendError("InternalError")
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
                    // No connection -> continue to CREATE flow on frontend
                    return RedirectFrontendPath($"/oauth/{Uri.EscapeDataString(provider)}/create");
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
                if (!User.TryGetAuthenticatedOpenShockUserId(out var userId))
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("mustBeAuthenticated");
                }

                if (connection is not null)
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendConnections(connection.UserId == userId ? "alreadyLinked" : "linkedToAnotherAccount");
                } 
                
                var ok = await connectionService.TryAddConnectionAsync(userId, provider, auth.ExternalAccountId, auth.ExternalAccountDisplayName ?? auth.ExternalAccountName, cancellationToken);
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
