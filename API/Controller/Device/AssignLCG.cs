using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Geo;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Device;

public sealed partial class DeviceController
{
    /// <summary>
    /// Gets the best suited LCG node for the client
    /// </summary>
    /// <response code="200">Successfully assigned LCG node</response>
    /// <response code="503">Unable to find suitable LCG node</response>
    [HttpGet("assignLCG")]
    [ProducesResponseType<LegacyDataResponse<LcgNodeResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.ProblemJson)] // NoLcgNodesAvailable
    public async Task<IActionResult> GetLiveControlGateway([FromServices] ILCGNodeProvisioner geoLocation,
        [FromServices] IWebHostEnvironment env)
    {
        if (!HttpContext.TryGetCFIPCountryCode(out var countryCode))
        {
            _logger.LogWarning("CF-IPCountry header could not be parsed into a alpha2 country code");
        }

        var closestNode = await geoLocation.GetOptimalNode(countryCode, env.EnvironmentName);
        if (closestNode == null) return Problem(AssignLcgError.NoLcgNodesAvailable);

        return LegacyDataOk(new LcgNodeResponse
        {
            Fqdn = closestNode.Fqdn,
            Country = closestNode.Country
        });
    }
}