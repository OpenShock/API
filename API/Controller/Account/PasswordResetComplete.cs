﻿using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Complete a password reset process
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <param name="body"></param>
    /// <response code="200">Password successfully changed</response>
    /// <response code="404">Password reset process not found</response>
    [HttpPost("recover/{passwordResetId}/{secret}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PasswordResetNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> PasswordResetComplete([FromRoute] Guid passwordResetId,
        [FromRoute] string secret, [FromBody] PasswordResetProcessData body)
    {
        var passwordResetComplete = await _accountService.PasswordResetComplete(passwordResetId, secret, body.Password);

        return passwordResetComplete.Match(
            success => LegacyEmptyOk("Password successfully changed"),
            notFound => Problem(PasswordResetError.PasswordResetNotFound),
            invalid => Problem(PasswordResetError.PasswordResetNotFound));
    }
    

    public sealed class PasswordResetProcessData
    {
        public required string Password { get; init; }
    }
}