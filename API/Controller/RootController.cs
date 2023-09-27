using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon;

namespace OpenShock.API.Controller;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}")]
public class RootController : OpenShockControllerBase
{
    private static readonly string OpenShockBackendVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "error";

    [HttpGet]
    public BaseResponse<RootResponse> Get() => new()
    {
        Message = "OpenShock",
        Data = new RootResponse
        {
            Version = OpenShockBackendVersion,
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