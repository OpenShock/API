using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using OpenShock.Common.Errors;

namespace OpenShock.Common.ExceptionHandle;

public sealed class OpenShockExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger _logger;
    
    public OpenShockExceptionHandler(IProblemDetailsService problemDetailsService, ILoggerFactory loggerFactory)
    {
        _problemDetailsService = problemDetailsService;
        _logger = loggerFactory.CreateLogger("RequestInfo");
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
    
    private async Task PrintRequestInfo(HttpContext context)
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
        _logger.LogInformation("{@RequestInfo}", requestInfo);
    }
}