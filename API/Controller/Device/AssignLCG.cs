using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Geo;
using OpenShock.ServicesCommon.Problems;
using System.Net;
using System.Text;

namespace OpenShock.API.Controller.Device;

public sealed partial class DeviceController
{
    /// <summary>
    /// Gets the best suited LCG node for the client
    /// </summary>
    /// <response code="200">Successfully assigned LCG node</response>
    /// <response code="503">Unable to find suitable LCG node</response>
    [HttpGet("assignLCG")]
    [ProducesSuccess<LcgNodeResponse>]
    [ProducesProblem(HttpStatusCode.ServiceUnavailable, "NoLcgNodesAvailable")]
    public async Task<IActionResult> GetLiveControlGateway([FromServices] IGeoLocation geoLocation,
        [FromServices] IWebHostEnvironment env)
    {
        var messageBuilder = new StringBuilder();
        var countryCode = CountryCodeMapper.Alpha2CountryCode.DefaultAlphaCode;
        if (HttpContext.Request.Headers.TryGetValue("CF-IPCountry", out var countryHeader) &&
            !string.IsNullOrEmpty(countryHeader))
        {
            var countryHeaderString = countryHeader.ToString();
            if (CountryCodeMapper.Alpha2CountryCode.TryParseAndValidate(countryHeaderString, out var code))
            {
                countryCode = code;
            }
            else
            {
                _logger.LogWarning("Country alpha2 code could not be parsed [{CountryHeader}]", countryHeaderString);
                messageBuilder.AppendLine("Invalid alpha2 country code, default country used.");
            }
        }
        else
        {
            _logger.LogWarning("CF-IPCountry header could not be parsed");
            messageBuilder.AppendLine("No CF-IPCountry header found, default country used.");
        }

        var closestNode = await geoLocation.GetClosestNode(countryCode, env.EnvironmentName);

        if (closestNode == null) return Problem(AssignLcgError.NoLcgNodesAvailable);

        return RespondSuccess(new LcgNodeResponse
        {
            Fqdn = closestNode.Fqdn,
            Country = closestNode.Country
        }, messageBuilder.ToString());
    }
}