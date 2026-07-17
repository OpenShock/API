using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Mime;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Results;
using AccountSvc = OpenShock.API.Services.Account;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Deactivate currently logged in account
    /// </summary>
    /// <response code="204">Done.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // CannotDeactivatePrivledgedAccount
    public async Task<IActionResult> Deactivate()
    {
        var deactivationResult = await _accountService.DeactivateAccountAsync(CurrentUser.Id, CurrentUser.Id, deleteLater: true);
        return deactivationResult switch
        {
            Success => NoContent(),
            CannotDeactivatePrivilegedAccount => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            AccountDeactivationAlreadyInProgress => Problem(AccountActivationError.AlreadyDeactivated),
            AccountSvc.Unauthorized => Problem(AccountActivationError.Unauthorized),
            Results.NotFound => throw new Exception("This is not supposed to happen, wtf?"),
            _ => throw new UnreachableException()
        };
    }
}