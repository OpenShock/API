using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Mailjet;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Account;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account/recover/{id:guid}/{secret}")]
public class RecoverController : ShockLinkControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly ILogger<RecoverController> _logger;
    private readonly IMailjetClient _mailjetClient;

    public RecoverController(ShockLinkContext db, ILogger<RecoverController> logger, IMailjetClient mailjetClient)
    {
        _db = db;
        _logger = logger;
        _mailjetClient = mailjetClient;
    }

    [HttpHead]
    public async Task<BaseResponse<object>> CheckForReset(Guid id, string secret)
    {
        var reset = await _db.PasswordResets.SingleOrDefaultAsync(x =>
            x.Id == id && x.UsedOn == null && x.CreatedOn.AddDays(7) > DateTime.UtcNow);

        if (reset == null || !SecurePasswordHasher.Verify(secret, reset.Secret, customName: "PWRESET"))
            return EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);
        
        return new BaseResponse<object>();
    }

    [HttpPost]
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