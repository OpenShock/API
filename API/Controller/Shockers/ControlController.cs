using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers/control")]
public class ControlController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public ControlController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<BaseResponse<object>> Control(Control data)
    {
        return new BaseResponse<object>();
    }
}