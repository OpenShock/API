using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon;
using OpenShock.ServicesCommon.Problems;

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
    public BaseResponse<RootResponse> GetBackendVersion()
    {
        
        return new BaseResponse<RootResponse>
        {
            Message = "OpenShock",
            Data = new RootResponse
            {
                Version = OpenShockBackendVersion,
                Commit = GitHashAttribute.FullHash,
                CurrentTime = DateTimeOffset.UtcNow
            }
        };
    }

    public class RootResponse
    {
        public required string Version { get; set; }
        public required string Commit { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
    }
}