using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using System.Net.Mime;
using System.Reflection;

namespace OpenShock.API.Controller.Version;


/// <summary>
/// Version stuff
/// </summary>
[ApiController]
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
    [ProducesResponseType<BaseResponse<RootResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetBackendVersion(
        [FromServices] IOptions<FrontendOptions> frontendOptions,
        [FromServices] IOptions<CloudflareTurnstileOptions> turnstileOptions
        )
    {
        var frontendConfig = frontendOptions.Value;
        var turnstileConfig = turnstileOptions.Value;

        return RespondSuccessLegacy(
            data: new RootResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow,
                FrontendUrl = frontendConfig.BaseUrl,
                ShortLinkUrl = frontendConfig.ShortUrl,
                TurnstileSiteKey = turnstileConfig.SiteKey
            },
            message: "OpenShock"
        );
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