using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using System.Net;

namespace OpenShock.API.Controller.Account;

partial class AccountController
{
    /// <summary>
    /// Checks if a password reset process is valid
    /// </summary>
    /// <param name="id">The id of the password reset process</param>
    /// <param name="secret">The secret of the password reset process</param>
    /// <response code="200">Valid password reset process</response>
    /// <response code="404">Password reset process not found</response>
    [HttpHead("recover/{id}/{secret}", Name = "IsRecoverValid")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> CheckForReset([FromRoute] Guid id, [FromRoute] string secret)
    {
        var reset = await _db.PasswordResets.SingleOrDefaultAsync(x =>
            x.Id == id && x.UsedOn == null && x.CreatedOn.AddDays(7) > DateTime.UtcNow);

        if (reset == null || !SecurePasswordHasher.Verify(secret, reset.Secret, customName: "PWRESET"))
            return EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);

        return new BaseResponse<object>();
    }

    /// <summary>
    /// Performs a password reset
    /// </summary>
    /// <param name="id">The id of the password reset process</param>
    /// <param name="secret">The secret of the password reset process</param>
    /// <param name="data"></param>
    /// <response code="200">Password successfully changed</response>
    /// <response code="404">Password reset process not found</response>
    [HttpPost("recover/{id}/{secret}", Name = "PerformRecover")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> UseRecover([FromRoute] Guid id, [FromRoute] string secret, [FromBody] PasswordResetProcessData data)
    {
        var reset = await _db.PasswordResets.Include(x => x.User).SingleOrDefaultAsync(x =>
            x.Id == id && x.UsedOn == null && x.CreatedOn.AddDays(7) > DateTime.UtcNow);

        if (reset == null || !SecurePasswordHasher.Verify(secret, reset.Secret, customName: "PWRESET"))
            return EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);

        reset.UsedOn = DateTime.UtcNow;
        reset.User.Password = SecurePasswordHasher.Hash(data.Password);
        await _db.SaveChangesAsync();

        return new BaseResponse<object>
        {
            Message = "Successfully changed password"
        };
    }

    public class PasswordResetProcessData
    {
        public required string Password { get; set; }
    }
}