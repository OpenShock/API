using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Token;
using OpenShock.Common.Authentication;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
public sealed class TokenDeleteController : AuthenticatedSessionControllerBase
{
    private readonly IApiTokenService _tokenService;
    private readonly ILogger<TokenDeleteController> _logger;

    public TokenDeleteController(IApiTokenService tokenService, ILogger<TokenDeleteController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Revoke a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="auditService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Successfully deleted token</response>
    /// <response code="403">Api Token is not allowed to delete other Api Tokens</response>
    /// <response code="404">The token does not exist, or you do not have access to it</response>
    [HttpDelete("{tokenId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound    
    public async Task<IActionResult> DeleteToken(
        [FromRoute] Guid tokenId,
        [FromServices] IAuditService auditService,
        CancellationToken cancellationToken)
    {
        // If a token tries to delete itself, let it
        if (User.TryGetClaimValueAsGuid(OpenShockAuthClaims.ApiTokenId, out var currentApiTokenId) && currentApiTokenId == tokenId)
        {
            var info = await _tokenService.GetTokenAuditInfoAsync(tokenId, CurrentUser.Id, cancellationToken);
            if (await _tokenService.DeleteToken(tokenId, cancellationToken: cancellationToken))
            {
                if (info is not null)
                    await auditService.LogAsync(CurrentUser.Id, AuditAction.ApiTokenDeleted,
                        HttpContext.GetRemoteIP(), HttpContext.GetUserAgent(),
                        new ApiTokenDeletedMetadata(tokenId, info.Value.Name));
                return Ok();
            }

            _logger.LogWarning("Token {TokenId} attempted self-deletion but no record was found (possible race-condition).", tokenId);
            return Problem(ApiTokenError.ApiTokenNotFound);
        }

        var userIdentity = User.TryGetOpenShockUserIdentity();
        if (userIdentity is null) return Problem(ApiTokenError.ApiTokenCanOnlyDeleteSelf);

        // If a privileged user is trying to delete the token, let them (any owner)
        if (userIdentity.IsAdminOrSystem())
        {
            var info = await _tokenService.GetTokenAuditInfoAsync(tokenId, cancellationToken: cancellationToken);
            if (await _tokenService.DeleteToken(tokenId, cancellationToken: cancellationToken))
            {
                if (info is not null)
                    await auditService.LogAsync(info.Value.OwnerId, AuditAction.ApiTokenDeleted,
                        HttpContext.GetRemoteIP(), HttpContext.GetUserAgent(),
                        new ApiTokenDeletedMetadata(tokenId, info.Value.Name),
                        actorId: CurrentUser.Id);
                return Ok();
            }

            return Problem(ApiTokenError.ApiTokenNotFound);
        }

        // A normal user is trying to delete the token, delete it if they own it
        var userId = userIdentity.GetClaimValueAsGuid(ClaimTypes.NameIdentifier);
        var ownedInfo = await _tokenService.GetTokenAuditInfoAsync(tokenId, userId, cancellationToken);
        if (await _tokenService.DeleteToken(tokenId, userId, cancellationToken))
        {
            if (ownedInfo is not null)
                await auditService.LogAsync(userId, AuditAction.ApiTokenDeleted,
                    HttpContext.GetRemoteIP(), HttpContext.GetUserAgent(),
                    new ApiTokenDeletedMetadata(tokenId, ownedInfo.Value.Name));
            return Ok();
        }

        return Problem(ApiTokenError.ApiTokenNotFound);
    }
}
