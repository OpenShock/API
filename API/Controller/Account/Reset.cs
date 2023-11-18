using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Mailjet;
using OpenShock.API.Mailjet.Mail;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;

namespace OpenShock.API.Controller.Account;

partial class AccountController
{
    /// <summary>
    /// Sends a password reset email
    /// </summary>
    /// <param name="data"></param>
    /// <param name="mailjetClient"></param>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [HttpPost("reset")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<object>> ResetAction([FromBody] ResetRequest data, [FromServices] IMailjetClient mailjetClient)
    {
        var user = await _db.Users.Where(x => x.Email == data.Email.ToLowerInvariant()).Select(x => new
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

        await mailjetClient.SendMail(new TemplateMail
        {
            From = Contact.AccountManagement,
            Subject = "Password reset request",
            To = new[]
            {
                new Contact
                {
                    Email = user.User.Email,
                    Name = user.User.Name
                }
            },
            TemplateId = 4903722,
            Variables = new Dictionary<string, string>
            {
                {"link", new Uri(APIGlobals.ApiConfig.FrontendBaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{secret}").ToString() },
            }

        });

        return SendResponse();
    }

    private static BaseResponse<object> SendResponse() => new("Password reset has been sent via email if the email is associated to an registered account");

    public class ResetRequest
    {
        public required string Email { get; set; }
    }
}