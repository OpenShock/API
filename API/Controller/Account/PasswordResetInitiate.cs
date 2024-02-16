﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset
    /// </summary>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [HttpPost("reset")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<object>> PasswordResetInitiate([FromBody] ResetRequest body, [FromServices] IEmailService emailService)
    {
        var user = await _db.Users.Where(x => x.Email == body.Email.ToLowerInvariant()).Select(x => new
        {
            User = x,
            PasswordResetCount = x.PasswordResets.Count(y => y.UsedOn == null && y.CreatedOn.AddDays(7) > DateTime.UtcNow)
        }).FirstOrDefaultAsync();
        if (user == null || user.PasswordResetCount >= 3) return SendResponse();

        var secret = CryptoUtils.RandomString(32);
        var hash = SecurePasswordHasher.Hash(secret, customName: "PWRESET");
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            Secret = hash,
            User = user.User
        };
        _db.PasswordResets.Add(passwordReset);
        await _db.SaveChangesAsync();

        await emailService.PasswordReset(new Contact(user.User.Email, user.User.Name),
            new Uri(APIGlobals.ApiConfig.FrontendBaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{secret}"));

        return SendResponse();
    }

    private static BaseResponse<object> SendResponse() => new("Password reset has been sent via email if the email is associated to an registered account");

    public sealed class ResetRequest
    {
        public required string Email { get; set; }
    }
}