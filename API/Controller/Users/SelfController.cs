using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Users;

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