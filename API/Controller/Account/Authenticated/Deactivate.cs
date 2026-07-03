using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

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
        return deactivationResult.Match<IActionResult>(
            success => NoContent(),
            cannotDeactivatePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            alreadyDeactivated => Problem(AccountActivationError.AlreadyDeactivated),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => throw new Exception("This is not supposed to happen, wtf?")
        );
    }
}