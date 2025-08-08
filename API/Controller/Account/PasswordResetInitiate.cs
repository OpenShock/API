using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset
    /// </summary>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [HttpPost("reset")]
    [EnableRateLimiting("auth")]
    [MapToApiVersion("1")]
    public async Task<LegacyEmptyResponse> PasswordResetInitiate([FromBody] ResetRequest body)
    {
        await _accountService.CreatePasswordResetFlowAsync(body.Email);
        return new LegacyEmptyResponse("Password reset has been sent via email if the email is associated to an registered account");
    }

    public sealed class ResetRequest
    {
        [EmailAddress(true)]
        public required string Email { get; init; }
    }
}