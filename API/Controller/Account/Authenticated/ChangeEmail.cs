using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

using OpenShock.Internal.Common.Utils;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Initiate an email change for the current user. A verification link is sent to the new
    /// address; the change is not applied until that link is opened.
    /// </summary>
    /// <param name="body"></param>
    [HttpPost("email-change")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // EmailChangeUnchanged
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // AccountOAuthOnly
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // PasswordChangeInvalidPassword
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailChangeAlreadyInUse
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)] // EmailChangeTooMany
    // notActivated / deactivated / notFound are blocked by UserSessionAuthentication before reaching this controller.
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest body)
    {
        if (string.IsNullOrEmpty(CurrentUser.PasswordHash))
        {
            return Problem(AccountError.AccountOAuthOnly);
        }

        if (!HashingUtils.VerifyPassword(body.CurrentPassword, CurrentUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }

        var result = await _accountService.CreateEmailChangeFlowAsync(CurrentUser.Id, body.Email);

        return result.Match<IActionResult>(
            success => Ok(),
            alreadyInUse => Problem(AccountError.EmailChangeAlreadyInUse),
            unchanged => Problem(AccountError.EmailChangeUnchanged),
            tooMany => Problem(AccountError.EmailChangeTooMany),
            notActivated => throw new UnreachableException("Authenticated user is not activated"),
            deactivated => throw new UnreachableException("Authenticated user is deactivated"),
            notFound => throw new UnreachableException("Authenticated user not found in database"));
    }
}
