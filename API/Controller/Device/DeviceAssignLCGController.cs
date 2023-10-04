using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Geo;

namespace OpenShock.API.Controller.Device;

[ApiController]
[Route("/{version:apiVersion}/device/assignLCG")]
public sealed class DeviceAssignLcgController : AuthenticatedDeviceControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IGeoLocation _geoLocation;
    private readonly ILogger<DeviceAssignLcgController> _logger;

    public DeviceAssignLcgController(OpenShockContext db, IGeoLocation geoLocation, ILogger<DeviceAssignLcgController> logger)
    {
        _db = db;
        _geoLocation = geoLocation;
        _logger = logger;
    }

    [HttpGet]
    public async Task<BaseResponse<LcgNodeResponse>> Get()
    {
        var messageBuilder = new StringBuilder();
        var countryCode = CountryCodeMapper.CountryInfo.Alpha2CountryCode.DefaultAlphaCode;
        if (HttpContext.Request.Headers.TryGetValue("CF-IPCountry", out var countryHeader) && !string.IsNullOrEmpty(countryHeader))
        {
            var countryHeaderString = countryHeader.ToString();
            if (CountryCodeMapper.CountryInfo.Alpha2CountryCode.TryParseAndValidate(countryHeaderString, out var code))
                countryCode = code;
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

        if (CountryCodeMapper.CountryCodeToCountryInfo.TryGetValue(countryCode, out var country))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Client country identified as [{@CountryInfo}]", country);            
        }
        else
        {
            country = CountryCodeMapper.DefaultCountry;
            _logger.LogWarning("Country not found in mapping [{Alpha2Code}]", countryCode);
            messageBuilder.AppendLine("Country not found in mapping, default country used.");
        }
        
        var closestNode = await _geoLocation.GetClosestNode(country);

        if (closestNode == null)
            return EBaseResponse<LcgNodeResponse>("No LCG nodes available", HttpStatusCode.ServiceUnavailable);
        
        return new BaseResponse<LcgNodeResponse>
        {
            Message = messageBuilder.ToString(),
            Data = new LcgNodeResponse
            {
                Fqdn = closestNode.Fqdn,
                Country = closestNode.Country
            }
        };
    }
}