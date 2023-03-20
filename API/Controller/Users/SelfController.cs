using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users/self")]
public sealed class SelfController : AuthenticatedSessionControllerBase
{
    private static Guid DefaultAvatar = Guid.Parse("7d7302ba-be81-47bb-671d-33f9efd20900");
    private readonly ShockLinkContext _db;
    private readonly ILogger<SelfController> _logger;

    public SelfController(ShockLinkContext db, ILogger<SelfController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<BaseResponse<SelfResponse>> GetSelf()
    {
        return new BaseResponse<SelfResponse>
        {
            Data = new SelfResponse
            {
                Id = CurrentUser.DbUser.Id,
                Name = CurrentUser.DbUser.Name,
                Email = CurrentUser.DbUser.Email,
                Image = CurrentUser.GetImageLink()
            }
        };
    }

    [HttpPost("avatar")]
    public async Task<BaseResponse<object>> UpdateAvatar(IFormFile avatar)
    {
        if (avatar == null) return EBaseResponse<object>("No 'avatar' file has been attached");

        var oldImageId = CurrentUser.DbUser.Image;
        
        try
        {
            _logger.LogDebug("Uploading new avatar to cloudflare and making db entry");
            if (!await ImagesApi.UploadAvatar(CurrentUser.DbUser.Id, avatar.OpenReadStream(), _db))
                return EBaseResponse<object>("Error during image creation", HttpStatusCode.InternalServerError);
        }
        catch (ImagesApi.IncorrectImageFormatException exception)
        {
            _logger.LogWarning(exception, "Image format is incorrect");
            return EBaseResponse<object>("Image format must be PNG or JPG");
        }

        if (CurrentUser.DbUser.Image != DefaultAvatar)
        {
            // Delete old avatar from cloudflare and db
            _logger.LogDebug("Deleting old avatar from cloudflare and db");
            if(await _db.CfImages.Where(x => x.Id == oldImageId).ExecuteDeleteAsync() < 1)
                _logger.LogWarning("Trying to delete old avatar file out of db, but it couldn't be found. {Image}", oldImageId);

            await ImagesApi.DeleteImage(oldImageId);
        }

        return new BaseResponse<object>
        {
            Message = "Profile picture has been changed successfully"
        };
    }

    public class AvatarRequest
    {
        public required string Image { get; set; }
    }
    
    public class SelfResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required Uri Image { get; set; }
    }
}