using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using System.Net;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Check if a password reset is in progress
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <response code="200">Valid password reset process</response>
    /// <response code="404">Password reset process not found</response>
    [HttpHead("recover/{passwordResetId}/{secret}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> PasswordResetCheckValid([FromRoute] Guid passwordResetId, [FromRoute] string secret)
    {
        var reset = await _db.PasswordResets.SingleOrDefaultAsync(x =>
            x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn.AddDays(7) > DateTime.UtcNow);

        if (reset == null || !SecurePasswordHasher.Verify(secret, reset.Secret, customName: "PWRESET"))
            return EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);

        return new BaseResponse<object>();
    }
}