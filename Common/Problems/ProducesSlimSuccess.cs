using System.Net;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Annotations;

namespace OpenShock.ServicesCommon.Problems;

public sealed class ProducesSlimSuccess<T> : SwaggerResponseAttribute
{
    public ProducesSlimSuccess(string title = "", HttpStatusCode statusCode = HttpStatusCode.OK) : base((int)statusCode, title, typeof(T), MediaTypeNames.Application.Json)
    {
    }
}