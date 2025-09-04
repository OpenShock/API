using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.Constants;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Finalize an OAuth flow by either creating a new local account or linking to the current account.
    /// </summary>
    /// <remarks>
    /// Authenticates via the temporary OAuth flow cookie (set during the provider callback).
    /// - <b>create</b>: creates a local account, then links the external identity.<br/>
    /// - <b>link</b>: requires a logged-in local user; links the external identity to that user.<br/>
    /// No access/refresh tokens are returned.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="body">Finalize request.</param>
    /// <param name="accountService"></param>
    /// <param name="connectionService"></param>
    /// <response code="200">Finalization succeeded.</response>
    /// <response code="400">Flow not found, bad action, username invalid, or provider mismatch.</response>
    /// <response code="401">Link requested but user not authenticated.</response>
    /// <response code="409">External already linked to another account, or duplicate link attempt.</response>
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/finalize")]
    [ProducesResponseType(typeof(OAuthFinalizeResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status401Unauthorized, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status409Conflict, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthFinalize(
        [FromRoute] string provider,
        [FromBody] OAuthFinalizeRequest body,
        [FromServices] IAccountService accountService,
        [FromServices] IOAuthConnectionService connectionService)
    {
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);

        var action = body.Action?.Trim().ToLowerInvariant();
        if (action is not (AuthConstants.OAuthLoginOrCreateFlow or AuthConstants.OAuthLinkFlow))
            return Problem(OAuthError.UnsupportedFlow);

        // Authenticate via the short-lived OAuth flow cookie (temp scheme)
        var auth = await HttpContext.AuthenticateAsync(OAuthConstants.FlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return Problem(OAuthError.FlowNotFound);

        // Flow must belong to the same provider we’re finalizing
        var providerClaim = auth.Principal.FindFirst("provider")?.Value;
        if (!string.Equals(providerClaim, provider, StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ProviderMismatch);
        }

        // External identity basics from claims (added by your handler)
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = body.Email ?? auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = body.Username ?? auth.Principal.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(displayName))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }


        // If the external is already linked, don’t allow relinking in either flow.
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, externalId);

        if (action == AuthConstants.OAuthLinkFlow)
        {
            // Linking requires an authenticated session
            var userRef = HttpContext.RequestServices.GetRequiredService<IUserReferenceService>();
            if (userRef.AuthReference is null || !userRef.AuthReference.Value.IsT0)
            {
                // Not a logged-in session (could be API token or anonymous)
                return Problem(OAuthError.NotAuthenticatedForLink);
            }

            var currentUser = HttpContext.RequestServices
                .GetRequiredService<IClientAuthService<User>>()
                .CurrentClient;

            if (existing is not null)
            {
                // Already linked to someone, block.
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                return Problem(OAuthError.ExternalAlreadyLinked);
            }

            var ok = await connectionService.TryAddConnectionAsync(
                userId: currentUser.Id,
                provider: provider,
                providerAccountId: externalId,
                providerAccountName: displayName ?? email);

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

            if (!ok) return Problem(OAuthError.ExternalAlreadyLinked);

            return Ok(new OAuthFinalizeResponse
            {
                Provider = provider,
                ExternalId = externalId
            });
        }

        if (action is not AuthConstants.OAuthLoginOrCreateFlow)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.UnsupportedFlow);
        }

        if (existing is not null)
        {
            // External already mapped; treat as conflict (or you could return 200 if you consider this a no-op login).
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ConnectionAlreadyExists);
        }

        // We must create a local account. Your AccountService requires a password, so:
        var desiredUsername = body.Username?.Trim();
        if (string.IsNullOrEmpty(desiredUsername))
        {
            // Generate a reasonable username from displayName/email/externalId
            desiredUsername = GenerateUsername(displayName, email, externalId, provider);
        }

        // Ensure username is available; if not, try a few suffixes
        desiredUsername = await EnsureAvailableUsernameAsync(desiredUsername, accountService);

        var password = string.IsNullOrEmpty(body.Password)
            ? CryptoUtils.RandomString(32) // strong random (since OAuth-only users won't use it)
            : body.Password;

        var created = await accountService.CreateOAuthOnlyAccountAsync(
            email: email,
            username: body.Username!,
            provider: provider,
            providerAccountId: externalId,
            providerAccountName: displayName
        );


        if (created.IsT1)
        {
            return Problem(SignupError.UsernameOrEmailExists);
        }

        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        var newUser = created.AsT0.Value;

        return Ok(new OAuthFinalizeResponse
        {
            Provider = provider,
            ExternalId = externalId,
            Username = newUser.Name
        });

        // ------- local helpers --------

        static string GenerateUsername(string? name, string? mail, string externalId, string providerKey)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return Slugify(name);

            if (!string.IsNullOrWhiteSpace(mail))
            {
                var at = mail.IndexOf('@');
                if (at > 0) return Slugify(mail[..at]);
            }

            return $"{providerKey}_{externalId}".ToLowerInvariant();
        }

        static string Slugify(string s)
        {
            var slug = new string(s.Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray());
            slug = System.Text.RegularExpressions.Regex.Replace(slug, "_{2,}", "_").Trim('_');
            return string.IsNullOrEmpty(slug) ? "user" : slug;
        }

        static async Task<string> EnsureAvailableUsernameAsync(string baseName, IAccountService account)
        {
            var candidate = baseName;
            for (var i = 0; i < 10; i++)
            {
                var check = await account.CheckUsernameAvailabilityAsync(candidate);
                if (check.IsT0) return candidate; // Success
                candidate = $"{baseName}_{CryptoUtils.RandomString(4).ToLowerInvariant()}";
            }
            // last resort: include a timestamp suffix
            return $"{baseName}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }
    }
}
