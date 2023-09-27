using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.Serialization;

namespace OpenShock.ServicesCommon;

[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class ShockLinkControllerBase : Microsoft.AspNetCore.Mvc.Controller
{

    [NonAction]
    protected BaseResponse<T> EBaseResponse<T>(string message = "An unknown error occurred.",
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        Response.StatusCode = (int)statusCode;
        return new BaseResponse<T> { Message = message };
    }

    [NonAction]
    protected IActionResult EaBaseResponse(string message = "An unknown error occurred.",
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        Response.StatusCode = (int)statusCode;
        return new JsonResult(new BaseResponse<object>
        {
            Message = message
        }, Options.Default);
    }
}