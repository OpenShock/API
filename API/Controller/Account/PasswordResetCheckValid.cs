using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Account;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Internal.Common.Problems;
using Results = OpenShock.Common.Results;

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
    [ApiVersion(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
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
    [ApiVersion(1)]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PasswordResetNotFound
    public async Task<IActionResult> PasswordResetCheckValid([FromRoute] Guid passwordResetId, [FromRoute] string secret, CancellationToken cancellationToken)
    {
        var passwordResetExists = await _accountService.CheckPasswordResetExistsAsync(passwordResetId, secret, cancellationToken);
        return passwordResetExists switch
        {
            Results.Success => LegacyEmptyOk("Valid password reset process"),
            Results.NotFound => Problem(PasswordResetError.PasswordResetNotFound),
            SecretInvalid => Problem(PasswordResetError.PasswordResetNotFound)
        };
    }
}
