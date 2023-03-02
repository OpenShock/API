using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers/shared")]
public class SharedShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public SharedShockersController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<BaseResponse<IEnumerable<ShockerResponse>>> GetSharedShockers()
    {
        /*var sharedShocker = await _db.ShockerShares.Where(x => x.SharedWith == CurrentUser.DbUser.Id).Select(x =>
            new ShockerResponse
            {
                Id = x.Shocker.Id,
                Owner = x.Shocker.Owner,
                Name = x.Shocker.Name,
                RfId = x.Shocker.RfId
            }).ToListAsync();*/

        return new BaseResponse<IEnumerable<ShockerResponse>>
        {
            Data = null
        };
    }
}