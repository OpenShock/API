using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Public;

partial class PublicController
{
    [HttpGet("firmware/version")]
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