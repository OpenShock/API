using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Authentication;
using ShockLink.API.Models;

namespace ShockLink.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users/self")]
public class SelfController : AuthenticatedSessionControllerBase
{
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
                Image = new Uri("https://sea.zlucplayz.com/f/e18b174d56db47759384/?raw=1")
            }
        };
    }
    
    public class SelfResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required Uri Image { get; set; }
    }
}