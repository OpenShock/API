using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.ServicesCommon.Errors;

namespace OpenShock.ServicesCommon.ExceptionHandle;

public static class ExceptionHandler
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(ExceptionHandler));
    private static readonly ILogger LoggerRequestInfo = ApplicationLogging.CreateLogger("RequestInfo");
    private const string CorrelationIdItem = "CorrelationIdEnricher+CorrelationId";

    /// <summary>
    /// Configures two middlewares used to handle exceptions globally.
    /// </summary>
    /// <param name="app"></param>
    public static void ConfigureExceptionHandler(this IApplicationBuilder app)
    {
        // Enable request body buffering. Needed to allow rewinding the body reader,
        // if the body has already been read before.
        // Runs before the request action is executed and body is read.
        app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next.Invoke();
        });
        
        // Use the built in exception handler middleware, to capture unhandled exceptions.
        app.UseExceptionHandler(appError =>
        {
            // Action to be executed when an exception is thrown.
            appError.Run(async context =>
            {
                // Get the exception details by getting the IExceptionHandlerFeature from our HttpContext.
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                // This should not be null, otherwise no exception was thrown or some other error in ASP.NET occurred.
                if (contextFeature == null) return;
                
                // Get our logging correlationId from out HttpContext's items.
                var correlationId = Guid.TryParse(context.Items[CorrelationIdItem]?.ToString(), out var correlationIdGuid) 
                    ? correlationIdGuid : Guid.Empty;

                // Any other exception has been thrown, return a InternalServerError, always print full request infos.
                // Side note: Exception logging is done by Microsoft Diagnostics middleware already, so no need to do
                // it again manually.
                
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await PrintRequestInfo(context, correlationId);
                
                
                var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
                
                var responseObject = ExceptionError.Exception;
                responseObject.AddContext(context);
                
                await context.Response.WriteAsJsonAsync(responseObject, jsonOptions.Value.SerializerOptions, contentType: "application/problem+json");
            });
        });
    }

    /// <summary>
    /// This method prints all relevant info that could be useful for debugging purposes.
    /// Also contains redundant data like method, path and correlation id for readability purposes.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="correlationId"></param>
    private static async Task PrintRequestInfo(HttpContext context, Guid correlationId)
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
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            Query = queryParams
        };
        
        // Finally log this object on Information level. 
        LoggerRequestInfo.LogInformation("{@RequestInfo}", requestInfo);
    }
}