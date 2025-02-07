using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deletes an API token
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("apitokens/{tokenId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteApiToken([FromRoute] Guid tokenId, CancellationToken cancellationToken)
    {
        var nDeleted = await _db.ApiTokens.Where(x => x.Id == tokenId).ExecuteDeleteAsync(cancellationToken);

        return nDeleted == 0 ? NotFound() : Ok();
    }
}