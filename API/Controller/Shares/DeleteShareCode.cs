using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    /// <summary>
    /// Delete a share code
    /// </summary>
    /// <param name="shareCodeId"></param>
    /// <response code="200">Deleted share code</response>
    /// <response code="404">Share code not found or does not belong to you</response>
    [HttpDelete("code/{shareCodeId}")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> DeleteShareCode([FromRoute] Guid shareCodeId)
    {
        var yes = await _db.ShockerShareCodes
            .Where(x => x.Id == shareCodeId && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).SingleOrDefaultAsync();
        var affected = await _db.ShockerShareCodes.Where(x =>
            x.Id == shareCodeId && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Share code does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        return new BaseResponse<object>("Successfully deleted share code");
    }
}