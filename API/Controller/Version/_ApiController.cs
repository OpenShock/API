using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using System.Reflection;
using System.Text;
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
    private static string GetBackendVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is null) return "0.0.0";

        var fieldCount = 3;
        if (version.Revision != 0) fieldCount = 4;
        
        return version.ToString(fieldCount);
    }
    
    private static readonly string OpenShockBackendVersion = GetBackendVersion();

    /// <summary>
    /// Gets the version of the OpenShock backend.
    /// </summary>
    /// <response code="200">The version was successfully retrieved.</response>
    [HttpGet]
    public LegacyDataResponse<BackendInfoResponse> GetBackendInfo(
        [FromServices] FrontendOptions frontendOptions,
        [FromServices] TurnstileOptions turnstileOptions
        )
    {
        return new(
            new BackendInfoResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow,
                FrontendUrl = frontendOptions.BaseUrl,
                ShortLinkUrl = frontendOptions.ShortUrl,
                TurnstileSiteKey = turnstileOptions.SiteKey,
                IsUserAuthenticated = HttpContext.TryGetUserSessionToken(out _)
            },
            "OpenShock"
        );
    }

    public sealed class BackendInfoResponse
    {
        public required string Version { get; init; }
        public required string Commit { get; init; }
        public required DateTimeOffset CurrentTime { get; init; }
        public required Uri FrontendUrl { get; init; }
        public required Uri ShortLinkUrl { get; init; }
        public required string? TurnstileSiteKey { get; init; }
        public required bool IsUserAuthenticated { get; init; }
    }
}