using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Utils;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Version;


/// <summary>
/// Version stuff
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}")]
public sealed partial class VersionController : OpenShockControllerBase
{
    private static readonly string OpenShockBackendVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "error";

    /// <summary>
    /// Gets the version of the OpenShock backend.
    /// </summary>
    /// <response code="200">The version was successfully retrieved.</response>
    [HttpGet]
    [ProducesSuccess<RootResponse>]
    public BaseResponse<RootResponse> GetBackendVersion([FromServices] ApiConfig apiConfig)
    {
        
        return new BaseResponse<RootResponse>
        {
            Message = "OpenShock",
            Data = new RootResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow,
                FrontendUrl = apiConfig.Frontend.BaseUrl,
                ShortLinkUrl = apiConfig.Frontend.ShortUrl,
                TurnstileSiteKey = apiConfig.Turnstile.SiteKey
            }
        };
    }

    public sealed class RootResponse
    {
        public required string Version { get; set; }
        public required string Commit { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
        public required Uri FrontendUrl { get; set; }
        public required Uri ShortLinkUrl { get; set; }
        public required string? TurnstileSiteKey { get; set; }
    }
}