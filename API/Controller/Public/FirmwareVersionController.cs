using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon;

namespace OpenShock.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public/firmware/version")]
[AllowAnonymous]
public class FirmwareVersionController : ShockLinkControllerBase
{
    [HttpGet]
    public BaseResponse<FirmwareVersion> Get()
    {
        return new BaseResponse<FirmwareVersion>
        {
            Data = new FirmwareVersion
            {
                Version = new Version(0, 8, 0),
                DownloadUri = new Uri("https://cdn.shocklink.net/firmware/shocklink_firmware_0.8.0.bin")
            }
        };
    }
}