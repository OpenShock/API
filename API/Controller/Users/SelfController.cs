using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users/self")]
public sealed class SelfController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<SelfController> _logger;

    public SelfController(OpenShockContext db, ILogger<SelfController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public BaseResponse<SelfResponse> GetSelf() => new()
    {
        Data = new SelfResponse
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Email = CurrentUser.DbUser.Email,
            Image = CurrentUser.GetImageLink(),
            Rank = CurrentUser.DbUser.Rank
        }
    };
    public class SelfResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required Uri Image { get; set; }
        public required RankType Rank { get; set; }
    }
}