using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.Models;

namespace ShockLink.API.Controller;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}")]
public class RootController : ShockLinkControllerBase
{
    private static readonly string ShockLinkBackendVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "error";

    [HttpGet]
    public BaseResponse<RootResponse> Get() => new()
    {
        Message = "ShockLink",
        Data = new RootResponse
        {
            Version = ShockLinkBackendVersion,
            Commit = GitHashAttribute.FullHash,
            CurrentTime = DateTimeOffset.UtcNow
        }
    };

    public class RootResponse
    {
        public required string Version { get; set; }
        public required string Commit { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
    }
}