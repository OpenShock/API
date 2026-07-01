using System;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Check if a password reset is in progress. Retired: use GET /1/account/password-reset/{passwordResetId}/{secret} instead.
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    [Obsolete("Retired. Use GET /1/account/password-reset/{passwordResetId}/{secret} instead.")]
    [HttpHead("recover/{passwordResetId}/{secret}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [MapToApiVersion("1")]
    public IActionResult PasswordResetCheckValidLegacy([FromRoute] Guid passwordResetId, [FromRoute] string secret)
        => Problem(GoneError.EndpointRetired("GET /1/account/password-reset/{passwordResetId}/{secret}"));
    
    /// <summary>
    /// Check if a password reset is in progress
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Valid password reset process</response>
    /// <response code="404">Password reset process not found</response>
    [HttpGet("password-reset/{passwordResetId}/{secret}")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PasswordResetNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> PasswordResetCheckValid([FromRoute] Guid passwordResetId, [FromRoute] string secret, CancellationToken cancellationToken)
    {
        var passwordResetExists = await _accountService.CheckPasswordResetExistsAsync(passwordResetId, secret, cancellationToken);
        return passwordResetExists.Match(
            success => LegacyEmptyOk("Valid password reset process"),
            notFound => Problem(PasswordResetError.PasswordResetNotFound),
            invalid => Problem(PasswordResetError.PasswordResetNotFound)
        );
    }
}