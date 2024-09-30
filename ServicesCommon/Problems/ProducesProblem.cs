using System.Net;
using Swashbuckle.AspNetCore.Annotations;

namespace OpenShock.ServicesCommon.Problems;

public class ProducesProblem<T> : SwaggerResponseAttribute where T : OpenShockProblem
{
    private const string ContentType = "application/problem+json";
    
    public ProducesProblem(HttpStatusCode statusCode, string title) : base((int)statusCode, title, typeof(T), ContentType)
    {
    }
}

public sealed class ProducesProblem : ProducesProblem<OpenShockProblem>
{
    public ProducesProblem(HttpStatusCode statusCode, string title) : base(statusCode, title)
    {
    }
}