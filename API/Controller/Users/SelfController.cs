using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.ShockLinkDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users/self")]
public sealed class SelfController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly ILogger<SelfController> _logger;

    public SelfController(ShockLinkContext db, ILogger<SelfController> logger)
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
            Image = CurrentUser.GetImageLink()
        }
    };
    public class SelfResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required Uri Image { get; set; }
    }
}