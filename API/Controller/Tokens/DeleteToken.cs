using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
public sealed class TokenDeleteController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<TokenDeleteController> _logger;

    public TokenDeleteController(OpenShockContext db, ILogger<TokenDeleteController> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    private async Task<bool> TryDeleteByIdAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        int nDeleted = await _db.ApiTokens.Where(x => x.Id == tokenId).ExecuteDeleteAsync(cancellationToken);
        return nDeleted > 0;
    }
    private async Task<bool> TryDeleteByIdAndOwnerAsync(Guid tokenId, Guid ownerId, CancellationToken cancellationToken)
    {
        int nDeleted = await _db.ApiTokens.Where(x => x.Id == tokenId && x.UserId == ownerId).ExecuteDeleteAsync(cancellationToken);
        return nDeleted > 0;
    }

    /// <summary>
    /// Revoke a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Successfully deleted token</response>
    /// <response code="403">Api Token is not allowed to delete other Api Tokens</response>
    /// <response code="404">The token does not exist, or you do not have access to it</response>
    [HttpDelete("{tokenId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound    
    public async Task<IActionResult> DeleteToken([FromRoute] Guid tokenId, CancellationToken cancellationToken)
    {
        // If a token tries to delete itself, let it
        if (User.TryGetClaimValueAsGuid(OpenShockAuthClaims.ApiTokenId, out var currentApiTokenId) && currentApiTokenId == tokenId)
        {
            if (await TryDeleteByIdAsync(tokenId, cancellationToken)) return Ok();
            
            // If we get here, it's a race-condition or something weird!
            _logger.LogWarning("Token {TokenId} attempted self-deletion but no record was found (possible race-condition).", tokenId);
            
            return Problem(ApiTokenError.ApiTokenNotFound);
        }
        
        var userIdentity = User.TryGetOpenShockUserIdentity();
        if (userIdentity is null) return Problem(ApiTokenError.ApiTokenCanOnlyDeleteSelf); // If user is null then ApiToken must have been here, and it cant delete others 
    
        // If a privileged user is trying to delete the token, let them
        if (userIdentity.IsAdminOrSystem())
        {
            if (await TryDeleteByIdAsync(tokenId, cancellationToken)) return Ok();
            
            return Problem(ApiTokenError.ApiTokenNotFound);
        }
        
        // A normal user is trying to delete the token, delete it if they own it
        var userId = userIdentity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier);
        if (await TryDeleteByIdAndOwnerAsync(tokenId, userId, cancellationToken))
        {
            return Ok();
        }
        
        return Problem(ApiTokenError.ApiTokenNotFound);
    }
}