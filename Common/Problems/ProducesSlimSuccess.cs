using System.Net;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Annotations;

namespace OpenShock.Common.Problems;

public class ProducesSlimSuccess<T> : SwaggerResponseAttribute
{
    public ProducesSlimSuccess(string title = "", HttpStatusCode statusCode = HttpStatusCode.OK) : base((int)statusCode, title, typeof(T), MediaTypeNames.Application.Json)
    {
    }
}

public sealed class ProducesSlimSuccess : ProducesSlimSuccess<object>
{
    public ProducesSlimSuccess(string title = "", HttpStatusCode statusCode = HttpStatusCode.OK) : base(title, statusCode)
    {
    }
}