using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using OpenShock.Common.Errors;

namespace OpenShock.Common.ExceptionHandle;

public sealed class OpenShockExceptionHandler : IExceptionHandler
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(OpenShockExceptionHandler));
    private static readonly ILogger LoggerRequestInfo = ApplicationLogging.CreateLogger("RequestInfo");
    
    private readonly IProblemDetailsService _problemDetailsService;

    public OpenShockExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }
    
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await PrintRequestInfo(context);
        
        var responseObject = ExceptionError.Exception;
        responseObject.AddContext(context);

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = responseObject
        });
    }
    
    private static async Task PrintRequestInfo(HttpContext context)
    {
        // Rewind our body reader, so we can read it again.
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        // Used to read from the body stream.
        using var stream = new StreamReader(context.Request.Body);

        // Create Dictionaries to be logging in our RequestInfo object for both Header values and Query parameters.
        var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());
        var queryParams = context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
        
        // Create our RequestInfo object.
        var requestInfo = new RequestInfo
        {
            Body = await stream.ReadToEndAsync(),
            Headers = headers,
            TraceId = context.TraceIdentifier,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            Query = queryParams
        };
        
        // Finally log this object on Information level. 
        LoggerRequestInfo.LogInformation("{@RequestInfo}", requestInfo);
    }
}