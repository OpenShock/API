using Microsoft.AspNetCore.Mvc;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Delete an OAuth connection by provider
    /// </summary>
    [HttpDelete("connections/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOAuthConnection([FromRoute] string provider)
    {
        var deleted = await _accountService.DeleteOAuthConnectionAsync(CurrentUser.Id, provider);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}