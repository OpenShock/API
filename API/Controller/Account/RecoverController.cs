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
    /// <param name="id"></param>
    /// <param name="secret"></param>
    /// <returns></returns>
    /// <response code="200">Valid password reset process</response>
    /// <response code="404">Password reset process not found</response>
    [HttpHead("/recover/{id}/{secret}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> CheckForReset(Guid id, string secret)
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
    /// <param name="id"></param>
    /// <param name="secret"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <response code="200">Password successfully changed</response>
    /// <response code="404">Password reset process not found</response>
    [HttpPost("/recover/{id}/{secret}")]
    public async Task<BaseResponse<object>> UseRecover(Guid id, string secret, PasswordResetProcessData data)
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