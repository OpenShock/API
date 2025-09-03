using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;
using System.Threading.Tasks;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/authorize")]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromQuery(Name = "return_to")] string returnTo)
    {
        var result = await _registry.StartAuthorizeAsync(HttpContext, provider, OAuthFlow.SignIn, returnTo);
        return result.Match<IActionResult>(
            uri => Redirect(uri.ToString()),
            error => Problem(title: error.Code, detail: error.Description),
            notSupported => Problem(OAuthError.ProviderNotSupported)
        );
    }
}