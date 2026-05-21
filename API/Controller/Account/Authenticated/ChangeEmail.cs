using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Initiate an email change for the current user. A verification link is sent to the new
    /// address; the change is not applied until that link is opened.
    /// </summary>
    /// <param name="body"></param>
    [HttpPost("email")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // EmailChangeUnchanged
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // PasswordChangeInvalidPassword
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailChangeAlreadyInUse
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)] // EmailChangeTooMany
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest body)
    {
        if (string.IsNullOrEmpty(CurrentUser.PasswordHash) || !HashingUtils.VerifyPassword(body.CurrentPassword, CurrentUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }

        var result = await _accountService.CreateEmailChangeFlowAsync(CurrentUser.Id, body.Email);

        return result.Match<IActionResult>(
            success => Ok(),
            alreadyInUse => Problem(AccountError.EmailChangeAlreadyInUse),
            unchanged => Problem(AccountError.EmailChangeUnchanged),
            tooMany => Problem(AccountError.EmailChangeTooMany),
            deactivated => Problem(AccountError.AccountDeactivated),
            notFound => throw new UnreachableException("Authenticated user not found in database"));
    }
}
