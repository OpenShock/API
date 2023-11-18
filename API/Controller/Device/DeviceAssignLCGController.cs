using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Geo;
using System.Net;
using System.Text;

namespace OpenShock.API.Controller.Device;

partial class DeviceController
{
    [HttpGet("assignLCG")]
    public async Task<BaseResponse<LcgNodeResponse>> Get([FromServices] IGeoLocation geoLocation)
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

        var closestNode = await geoLocation.GetClosestNode(country);

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