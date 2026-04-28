using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.API.Services.LCGNodeProvisioner;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
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
    [MapToApiVersion("1")]
    [ProducesResponseType<LegacyDataResponse<LcgNodeResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.ProblemJson)] // NoLcgNodesAvailable
    public async Task<IActionResult> GetLiveControlGateway([FromServices] ILCGNodeProvisioner geoLocation)
    {
        if (!HttpContext.TryGetCFIPCountryCode(out var countryCode))
        {
            _logger.LogWarning("CF-IPCountry header could not be parsed into a alpha2 country code");
        }

        try {
        var closestNode = await geoLocation.GetOptimalNodeAsync(countryCode);
        if (closestNode is null) return Problem(AssignLcgError.NoLcgNodesAvailable);

        return LegacyDataOk(new LcgNodeResponse
        {
            Fqdn = closestNode.Fqdn,
            Country = closestNode.Country
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while assigning LCG node");
            return Problem(AssignLcgError.NoLcgNodesAvailable);
        }
    }
}