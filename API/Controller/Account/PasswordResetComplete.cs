using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using System.Net;
using Asp.Versioning;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Complete a password reset process
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <param name="body"></param>
    /// <response code="200">Password successfully changed</response>
    /// <response code="404">Password reset process not found</response>
    [HttpPost("recover/{passwordResetId}/{secret}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> PasswordResetComplete([FromRoute] Guid passwordResetId, [FromRoute] string secret, [FromBody] PasswordResetProcessData body)
    {
        var reset = await _db.PasswordResets.Include(x => x.User).SingleOrDefaultAsync(x =>
            x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn.AddDays(7) > DateTime.UtcNow);

        if (reset == null || !SecurePasswordHasher.Verify(secret, reset.Secret, customName: "PWRESET"))
            return EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);

        reset.UsedOn = DateTime.UtcNow;
        reset.User.Password = SecurePasswordHasher.Hash(body.Password);
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