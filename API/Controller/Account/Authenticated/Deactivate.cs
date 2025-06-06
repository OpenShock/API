﻿using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Deactivate currently logged in account
    /// </summary>
    /// <response code="200">Done.</response>
    [HttpDelete]
    [ProducesResponseType<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // CannotDeactivatePrivledgedAccount
    public async Task<IActionResult> Deactivate()
    {
        var deactivationResult = await _accountService.DeactivateAccount(CurrentUser.Id, CurrentUser.Id, deleteLater: true);
        return deactivationResult.Match(
            success => Ok("Account deactivated"),
            cannotDeactivatePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            alreadyDeactivated => Problem(AccountActivationError.AlreadyDeactivated),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => throw new Exception("This is not supposed to happen, wtf?")
        );
    }
}