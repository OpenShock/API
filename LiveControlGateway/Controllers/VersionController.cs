using Microsoft.AspNetCore.Mvc;
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
    private static readonly AssemblyName AssemblyName = typeof(VersionController).Assembly.GetName();
    private static readonly string AssemblyVersion = AssemblyName.Version?.ToString() ?? "error";
    private static readonly string AssemblyNameString = AssemblyName.Name ?? "error";

    /// <summary>
    /// Gets the version of the OpenShock backend.
    /// </summary>
    /// <response code="200">The version was successfully retrieved.</response>
    [HttpGet]
    [ProducesSlimSuccess<RootResponse>]
    [MapToApiVersion("1")]
    public RootResponse GetBackendVersion()
    {
        return new RootResponse
        {
            Name = AssemblyNameString,
            Version = AssemblyVersion,
            CurrentTime = DateTimeOffset.UtcNow,
            Fqdn = LCGGlobals.LCGConfig.Fqdn,
            CountryCode = LCGGlobals.LCGConfig.CountryCode
        };
    }

    public sealed class RootResponse
    {
        public required string Name { get; set; }
        public required string Version { get; set; }
        public required DateTimeOffset CurrentTime { get; set; }
        public required string CountryCode { get; set; }
        public required string Fqdn { get; set; }
    }
}