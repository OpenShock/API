using System.Net.Mime;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace OpenShock.Common.Websocket;

/// <summary>
/// Base for json serialized websocket controller, you can override the SendMessageMethod to implement a different serializer
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class WebsocketBaseController<T> : OpenShockControllerBase, IAsyncDisposable, IDisposable, IWebsocketController<T> where T : class
{
    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger<WebsocketBaseController<T>> Logger;

    /// <summary>
    /// Close cancellation token to be called manually when termination of the current websocket is requested. Called on Dispose as well.
    /// </summary>
    protected readonly CancellationTokenSource Close = new();

    /// <summary>
    /// When passing a cancellation token, pass this Linked token, it is a Link from ApplicationStopping and Close.
    /// </summary>
    protected readonly CancellationTokenSource LinkedSource;
    protected readonly CancellationToken LinkedToken;

    /// <summary>
    /// Channel for multithreading thread safety of the websocket, MessageLoop is the only reader for this channel
    /// </summary>
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

#pragma warning disable IDISP008
    protected WebSocket? WebSocket;
#pragma warning restore IDISP008

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    public WebsocketBaseController(ILogger<WebsocketBaseController<T>> logger, IHostApplicationLifetime lifetime)
    {
        Logger = logger;
        LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(Close.Token, lifetime.ApplicationStopping);
        LinkedToken = LinkedSource.Token;
    }


    /// <inheritdoc />
    [NonAction]
    public ValueTask QueueMessage(T data) => _channel.Writer.WriteAsync(data);

    private bool _disposed;

    /// <inheritdoc />
    [NonAction]
    public virtual void Dispose()
    {
        // ReSharper disable once MethodSupportsCancellation
        DisposeAsync().AsTask().Wait();
    }
    
    /// <inheritdoc />
    [NonAction]
    public virtual async ValueTask DisposeAsync()
    {
        if(_disposed) return;
        Logger.LogTrace("Disposing websocket controller..");
        _disposed = true;
        await DisposeControllerAsync();
        await ConnectionDestroyed();
        
        _channel.Writer.Complete();
        await Close.CancelAsync();
        WebSocket?.Dispose();
        LinkedSource.Dispose();
        
        GC.SuppressFinalize(this);
        Logger.LogTrace("Disposed websocket controller");
    }
    
    /// <summary>
    /// Dispose function for any inheriting controller
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public virtual ValueTask DisposeControllerAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Initial get request to the websocket route - rewrite to websocket connection
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            var jsonOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = WebsocketError.NonWebsocketRequest;
            response.AddContext(HttpContext);
            await HttpContext.Response.WriteAsJsonAsync(response, jsonOptions.Value.SerializerOptions, contentType: MediaTypeNames.Application.ProblemJson);
            await Close.CancelAsync();
            return;
        }

        var connectionPrecondition = await ConnectionPrecondition();
        if (connectionPrecondition.IsT1)
        {
            var jsonOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
            var response = connectionPrecondition.AsT1.Value;
            HttpContext.Response.StatusCode = response.Status ?? StatusCodes.Status400BadRequest;
            response.AddContext(HttpContext);
            await HttpContext.Response.WriteAsJsonAsync(response, jsonOptions.Value.SerializerOptions, contentType: MediaTypeNames.Application.ProblemJson);
            
            await Close.CancelAsync();
            return;
        }

        Logger.LogInformation("Opening websocket connection");
        
        WebSocket?.Dispose(); // This should never happen, but just in case
        WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        await ConnectionCreated();

#pragma warning disable CS4014
        LucTask.Run(MessageLoop);
#pragma warning restore CS4014

        await SendInitialData();
        
        await Logic();
        
        
        if(_disposed) return;
        
        await ConnectionDestroyed();

        await Close.CancelAsync();
    }

    #region Send Loop

    /// <summary>
    /// Message loop to send out messages in the channel
    /// </summary>
    [NonAction]
    private async Task MessageLoop()
    {
        await foreach (var msg in _channel.Reader.ReadAllAsync(LinkedToken))
        {
            try
            {
                await SendWebSocketMessage(msg, WebSocket!, LinkedToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending message to client - {Msg}", JsonSerializer.Serialize(msg));
                throw;
            }
        }
    }

    /// <summary>
    /// Implementation method for sending the message out to the websocket, you might also wanna apply serialization here
    /// </summary>
    /// <param name="message"></param>
    /// <param name="websocket"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [NonAction]
    protected virtual Task SendWebSocketMessage(T message, WebSocket websocket, CancellationToken cancellationToken) =>
        JsonWebSocketUtils.SendFullMessage(message, websocket, cancellationToken);


    #endregion

    /// <summary>
    /// Main receiver logic for the websocket
    /// </summary>
    /// <returns></returns>
    [NonAction]
    protected abstract Task Logic();
    
    /// <summary>
    /// Send initial data to the client
    /// </summary>
    /// <returns></returns>
    [NonAction]
    protected virtual Task SendInitialData() => Task.CompletedTask;

    /// <summary>
    /// Action when the websocket connection is created
    /// </summary>
    [NonAction]
    protected virtual Task ConnectionCreated() => Task.CompletedTask;

    /// <summary>
    /// Action when the websocket connection is finished or disposed
    /// </summary>
    [NonAction]
    protected virtual Task ConnectionDestroyed() => Task.CompletedTask;

    /// <summary>
    /// Action when the websocket connection is destroyed to unregister the connection to a websocket manager
    /// </summary>
    [NonAction]
    protected virtual Task<OneOf<Success, Error<OpenShockProblem>>> ConnectionPrecondition() =>
        Task.FromResult(OneOf<Success, Error<OpenShockProblem>>.FromT0(new Success()));
}