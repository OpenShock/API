using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using System.Net.Mime;
using OpenShock.API.OAuth;
using OpenShock.Common.Services.Session;
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
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
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
                    return RedirectFrontendPath($"oauth/{provider}/create");
                }

                // Direct sign-in
                var domain = GetCurrentCookieDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    return RedirectFrontendError("internalError");
                }

                await CreateSession(connection.UserId, "." + domain);

                // Flow cookie no longer needed
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

                // TODO: optionally send to a specific frontend route
                return RedirectFrontendPath("");
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

                    return RedirectFrontendError(connection.UserId == userId ? "alreadyLinked" : "linkedToAnotherAccount");
                } 
                
                bool ok =await connectionService.TryAddConnectionAsync(userId, provider, auth.ExternalAccountId, auth.ExternalAccountName, cancellationToken);
                if (!ok)
                {
                    
                }

                // No connection -> continue to LINK flow on frontend.
                // IMPORTANT: keep the flow cookie so frontend can finalize with it.
                return RedirectFrontendPath($"oauth/{provider}/link");
            }

            default:
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                return RedirectFrontendError("internalError");
        }

        // --- helpers ---

        RedirectResult RedirectFrontendPath(string relativeOrQuery)
        {
            // If caller passes only query (e.g. "oauth/error?error=x"), it still works;
            // if they pass empty string, it redirects to base.
            var target = relativeOrQuery switch
            {
                "" => frontendOptions.BaseUrl,
                _ when relativeOrQuery.StartsWith('?') => new Uri(frontendOptions.BaseUrl, "/" + relativeOrQuery), // force query on root
                _ => new Uri(frontendOptions.BaseUrl, relativeOrQuery.StartsWith('/') ? relativeOrQuery : "/" + relativeOrQuery)
            };
            return Redirect(target.ToString());
        }

        RedirectResult RedirectFrontendError(string errorType)
            => RedirectFrontendPath($"oauth/error?error={Uri.EscapeDataString(errorType)}");
    }
}
