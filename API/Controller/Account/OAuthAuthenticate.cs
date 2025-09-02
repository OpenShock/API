using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using System.Net.Mime;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Warning: This endpoint is not meant to be called by API clients, but only by the frontend.
    /// SSO authentication endpoint
    /// </summary>
    /// <param name="providerName">Name of the SSO provider to use, supported providers can be fetched from /api/v1/sso/providers</param>
    /// <param name="schemesProvider"></param>
    /// <status code="406">Not Acceptable, the SSO provider is not supported</status>
    [EnableRateLimiting("auth")]
    [EnableCors("allow_sso_providers")]
    [HttpGet("oauth/{providerName}", Name = "InternalSsoAuthenticate")]
    [HttpPost("oauth/{providerName}", Name = "InternalSsoCallback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    public async Task<IActionResult> OAuthAuthenticate([FromRoute] string providerName, [FromServices] IAuthenticationSchemeProvider schemesProvider)
    {
        if (!await schemesProvider.IsSupportedOAuthProviderAsync(providerName)) return HttpErrors.UnsupportedSSOProvider(providerName).ToActionResult();

        return Challenge(providerName);
    }
}
