using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Public;

public sealed partial class PublicController
{
    /// <summary>
    /// Gets the latest firmware version.
    /// </summary>
    /// <response code="200">The firmware version was successfully retrieved.</response>
    [HttpGet("firmware/version", Name = "GetFirmwareVersion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public BaseResponse<FirmwareVersion> GetFirmwareVersion()
    {
        return new BaseResponse<FirmwareVersion>
        {
            Data = new FirmwareVersion
            {
                Version = new Version(0, 8, 0),
                DownloadUri = new Uri("https://cdn.openshock.org/firmware/0.8.0.bin")
            }
        };
    }
}