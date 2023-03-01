using System.Net;
using Microsoft.AspNetCore.Mvc;
using ShockLink.API.Models;
using ShockLink.API.Serialization;

namespace ShockLink.API.Controller;

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