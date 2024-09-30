﻿using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Problems;
using System.Net;
using System.Net.Mime;

namespace OpenShock.ServicesCommon;

[Consumes(MediaTypeNames.Application.Json)]
[ProducesDoc]
public class OpenShockControllerBase : ControllerBase
{
    [NonAction]
    public ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);

    [NonAction]
    public ObjectResult RespondSuccess<T>(T data, string message = "", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Response.StatusCode = (int)statusCode;
        return new ObjectResult(new BaseResponse<T>
        {
            Data = data,
            Message = message
        });
    }

    [NonAction]
    public ObjectResult RespondSuccessSimple(string message = "", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Response.StatusCode = (int)statusCode;
        return new ObjectResult(new BaseResponse<object>
        {
            Message = message
        });
    }
}