using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using System.Reflection;

namespace OpenShock.API.Controller.Version;


/// <summary>
/// Version stuff
/// </summary>
[ApiController]
[Tags("Meta")]
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
    public LegacyDataResponse<ApiVersionResponse> GetBackendVersion(
        [FromServices] IOptions<FrontendOptions> frontendOptions,
        [FromServices] IOptions<CloudflareTurnstileOptions> turnstileOptions
        )
    {
        var frontendConfig = frontendOptions.Value;
        var turnstileConfig = turnstileOptions.Value;

        return new(
            new ApiVersionResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow,
                FrontendUrl = frontendConfig.BaseUrl,
                ShortLinkUrl = frontendConfig.ShortUrl,
                TurnstileSiteKey = turnstileConfig.SiteKey
            },
            "OpenShock"
        );
    }

    public sealed class ApiVersionResponse
    {
        public required string Version { get; set; }
        public required string Commit { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
        public required Uri FrontendUrl { get; set; }
        public required Uri ShortLinkUrl { get; set; }
        public required string? TurnstileSiteKey { get; set; }
    }
}