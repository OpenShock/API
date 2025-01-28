using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    /// <summary>
    /// Public reporting endpoint to invalidate potentially compromised tokens
    /// </summary>
    /// <param name="body"></param>
    /// <response code="200">The tokens were deleted if found</response>
    [HttpPost("invalidate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateTokens([FromBody] InvalidateTokensRequest body)
    {
        var hashes = body.Secrets.Select(HashingUtils.HashSha256).ToArray();
        await _db.ApiTokens.Where(x => hashes.Contains(x.TokenHash)).ExecuteDeleteAsync();

        return Ok();
    }
}