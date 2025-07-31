using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Deletes a public share
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <response code="200">Deleted public share</response>
    /// <response code="404">Public share not found or does not belong to you</response>
    [HttpDelete("{publicShareId}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound
    public async Task<IActionResult> DeletePublicShare([FromRoute] Guid publicShareId)
    {
        var result = await _db.PublicShares
            .Where(x => x.Id == publicShareId)
            .WhereIsUserOrPrivileged(x => x.Owner, CurrentUser)
            .ExecuteDeleteAsync();

        return result > 0
            ? LegacyEmptyOk("Deleted public share")
            : Problem(PublicShareError.PublicShareNotFound);
    }
}