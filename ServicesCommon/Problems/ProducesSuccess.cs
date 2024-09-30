using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using OpenShock.Common.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace OpenShock.ServicesCommon.Problems;

public class ProducesSuccess<T> : SwaggerResponseAttribute
{
    public ProducesSuccess(string title = "", HttpStatusCode statusCode = HttpStatusCode.OK) : base((int)statusCode, title, typeof(BaseResponse<T>), MediaTypeNames.Application.Json)
    {
    }
    
}

public sealed class ProducesSuccess : ProducesSuccess<object>
{
    public ProducesSuccess(string title = "", HttpStatusCode statusCode = HttpStatusCode.OK) : base(title, statusCode)
    {
    }
}