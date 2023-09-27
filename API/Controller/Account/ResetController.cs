using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Mailjet;
using OpenShock.API.Mailjet.Mail;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon;

namespace OpenShock.API.Controller.Account;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account/reset")]
public class ResetController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ResetController> _logger;
    private readonly IMailjetClient _mailjetClient;

    public ResetController(ILogger<ResetController> logger, OpenShockContext db, IMailjetClient mailjetClient)
    {
        _logger = logger;
        _db = db;
        _mailjetClient = mailjetClient;
    }

    [HttpPost]
    public async Task<BaseResponse<object>> ResetAction(ResetRequest data)
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
        
        await _mailjetClient.SendMail(new TemplateMail
        {
            From = Contact.AccountManagement,
            Subject = "Password reset request",
            To = new []
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