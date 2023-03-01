using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Authentication;
using ShockLink.API.Models;

namespace ShockLink.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users/self")]
public class SelfController : AuthenticatedSessionControllerBase
{
    [HttpGet]
    public async Task<BaseResponse<object>> GetSelf()
    {
        return new BaseResponse<object>();
    }
}