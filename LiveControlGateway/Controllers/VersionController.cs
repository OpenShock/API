using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Problems;
using System.Reflection;
using Asp.Versioning;
using OpenShock.ServicesCommon;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("/{version:apiVersion}")]
[ApiVersion("1")]
public sealed class VersionController : OpenShockControllerBase
{
    private static readonly string OpenShockBackendVersion =
        typeof(VersionController).Assembly.GetName().Version?.ToString() ?? "error";

    /// <summary>
    /// Gets the version of the OpenShock backend.
    /// </summary>
    /// <response code="200">The version was successfully retrieved.</response>
    [HttpGet]
    [ProducesSuccess<RootResponse>]
    [MapToApiVersion("1")]
    public BaseResponse<RootResponse> GetBackendVersion()
    {

        return new BaseResponse<RootResponse>
        {
            Message = "OpenShock",
            Data = new RootResponse
            {
                Version = OpenShockBackendVersion,
                CurrentTime = DateTimeOffset.UtcNow,
                Fqdn = LCGGlobals.LCGConfig.Fqdn,
                CountryCode = LCGGlobals.LCGConfig.CountryCode
            }
        };
    }

    public class RootResponse
    {
        public required string Version { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
        public required string CountryCode { get; set; }
        public required string Fqdn { get; set; }
    }
}