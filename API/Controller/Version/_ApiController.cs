using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using System.Reflection;
using OpenShock.API.Options;

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
        [FromServices] FrontendOptions frontendOptions,
        [FromServices] IOptions<TurnstileOptions> turnstileOptions
        )
    {
        var turnstileConfig = turnstileOptions.Value;

        return new(
            new ApiVersionResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow,
                FrontendUrl = frontendOptions.BaseUrl,
                ShortLinkUrl = frontendOptions.ShortUrl,
                TurnstileSiteKey = turnstileConfig.SiteKey,
            },
            "OpenShock"
        );
    }

    public sealed class ApiVersionResponse
    {
        public required string Version { get; init; }
        public required string Commit { get; init; }
        public required DateTimeOffset CurrentTime { get; init; }
        public required Uri FrontendUrl { get; init; }
        public required Uri ShortLinkUrl { get; init; }
        public required string? TurnstileSiteKey { get; init; }
    }
}