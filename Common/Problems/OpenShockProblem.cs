using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.Common.Problems;

/// <summary>
/// Represents a problem
/// </summary>
public class OpenShockProblem : ProblemDetails
{
    public OpenShockProblem(string type, string title, HttpStatusCode status,
        string? detail = null) : this(type, title, (int)status, detail)
    {
    }

    private OpenShockProblem(string type, string title, int status = 400, string? detail = null)
    {
        Type = type;
        Title = title;
        Detail = detail;
        Status = status;
    }
    
    [Obsolete("This is the exact same as title or detail if present, refer to using title in the future")]
    public string Message => Detail ?? Title!;
    
    [Obsolete("This is the exact same as requestId, refer to using requestId in the future")]
    public string? TraceId => RequestId;
    
    public string? RequestId { get; set; } 
    
    public ObjectResult ToObjectResult(HttpContext httpContext)
    {
        AddContext(httpContext);
        return new ObjectResult(this)
        {
            StatusCode = Status
        };
    }
    
    public void AddContext(HttpContext httpContext)
    {
        RequestId = httpContext.TraceIdentifier;
    }
}