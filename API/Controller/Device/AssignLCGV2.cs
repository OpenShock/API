using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Geo;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Device;

public sealed partial class DeviceController
{
    /// <summary>
    /// Gets the best suited LCG node for the client
    /// </summary>
    /// <response code="200">Successfully assigned LCG node</response>
    /// <response code="503">Unable to find suitable LCG node</response>
    [HttpGet("assignLCG")]
    [MapToApiVersion("2")]
    [ProducesResponseType<LcgNodeResponseV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // BadSchemaVersion
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.ProblemJson)] // NoLcgNodesAvailable
    public async Task<IActionResult> GetLiveControlGatewayV2([FromQuery(Name = "version")] uint version, [FromServices] ILCGNodeProvisioner geoLocation, [FromServices] IWebHostEnvironment env)
    {
        string path;
        switch (version)
        {
            case 1:
                path = "/1/ws/hub";
                break;
            case 2:
                path = "/2/ws/hub";
                break;
            default:
                return Problem(AssignLcgError.BadSchemaVersion);
        }

        if (!HttpContext.TryGetCFIPCountryCode(out var countryCode))
        {
            _logger.LogWarning("CF-IPCountry header could not be parsed into a alpha2 country code");
        }

        var closestNode = await geoLocation.GetOptimalNode(countryCode, env.EnvironmentName);
        if (closestNode is null) return Problem(AssignLcgError.NoLcgNodesAvailable);

        return Ok(new LcgNodeResponseV2
        {
            Host = closestNode.Fqdn,
            Port = 443,
            Path = path,
            Country = closestNode.Country
        });
    }
}