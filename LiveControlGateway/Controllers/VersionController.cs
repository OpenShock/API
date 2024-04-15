using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Problems;
using System.Reflection;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("/version")]
public sealed class VersionController : ControllerBase
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
                CurrentTime = DateTimeOffset.UtcNow
            }
        };
    }

    public class RootResponse
    {
        public required string Version { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
    }
}