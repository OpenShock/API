using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
public sealed class TokenDeleteController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<TokensController> _logger;

    public TokenDeleteController(OpenShockContext db, ILogger<TokensController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Revoke a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <response code="200">Successfully deleted token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpDelete("{tokenId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound,
        MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound    
    public async Task<IActionResult> DeleteToken([FromRoute] Guid tokenId)
    {
        var auth = HttpContext.GetAuthenticationMethod();

        var query = _db.ApiTokens.Where(x => x.Id == tokenId);


        switch (auth)
        {
            case OpenShockAuthSchemes.UserSessionCookie:
                query = query.WhereIsUserOrPrivileged(x => x.User, CurrentUser);
                break;
            case OpenShockAuthSchemes.ApiToken:
            {
                var requestTokenId = Guid.Parse(HttpContext.User.Claims.First(x => x.Type == OpenShockAuthClaims.ApiTokenId).Value);
                if (requestTokenId != tokenId) return Problem(ApiTokenError.ApiTokenCanOnlyDelete);
                break;
            }
            default:
                throw new Exception("Unknown auth method");
        }

        var apiToken = await query.ExecuteDeleteAsync();
        
        if (apiToken <= 0)
        {
            return Problem(ApiTokenError.ApiTokenNotFound);
        }

        return Ok();
    }
}