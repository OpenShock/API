using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.Common;

[Consumes(MediaTypeNames.Application.Json)]
public class OpenShockControllerBase : ControllerBase
{
    [NonAction]
    public ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);
    
    [NonAction]
    public ObjectResult RespondSuccessLegacy<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Response.StatusCode = (int)statusCode;
        return new ObjectResult(new LegacyDataResponse<T>(data));
    }
}