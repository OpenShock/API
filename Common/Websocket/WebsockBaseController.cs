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
public abstract class WebsocketBaseController<T> : OpenShockControllerBase, IAsyncDisposable, IDisposable,
    IWebsocketController<T> where T : class
{
    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger<WebsocketBaseController<T>> Logger;

    /// <summary>
    /// When passing a cancellation token, pass this Linked token, it is a Link from ApplicationStopping and Close.
    /// </summary>
    private CancellationTokenSource? _linkedSource;

    protected CancellationToken LinkedToken;
    
    /// <summary>
    /// Channel for multithreading thread safety of the websocket, MessageLoop is the only reader for this channel
    /// </summary>
    protected readonly Channel<T> Channel = System.Threading.Channels.Channel.CreateUnbounded<T>();

#pragma warning disable IDISP008
    protected WebSocket? WebSocket;
#pragma warning restore IDISP008

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    protected WebsocketBaseController(ILogger<WebsocketBaseController<T>> logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    [NonAction]
    public ValueTask QueueMessage(T data)
    {
        if (WebSocket is null or { State: WebSocketState.Closed or WebSocketState.CloseSent })
        {
            Logger.LogDebug("WebSocket is null or closed, not sending message");
            return ValueTask.CompletedTask;
        }
        
        return Channel.Writer.WriteAsync(data, LinkedToken);
    }

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
        if (_disposed) return;
        _disposed = true;

        Logger.LogTrace("Disposing websocket controller..");

        await DisposeControllerAsync();
        await UnregisterConnection();

        Channel.Writer.TryComplete();

        WebSocket?.Dispose();
        _linkedSource?.Dispose();

        GC.SuppressFinalize(this);
        Logger.LogTrace("Disposed websocket controller");
    }

    /// <summary>
    /// Dispose function for any inheriting controller
    /// </summary>
    /// <returns></returns>
    [NonAction]
    protected virtual ValueTask DisposeControllerAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Initial get request to the websocket route - rewrite to websocket connection
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    public async Task Get([FromServices] IHostApplicationLifetime lifetime, CancellationToken cancellationToken)
    {
#pragma warning disable IDISP003
        _linkedSource = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, cancellationToken);
#pragma warning restore IDISP003
        LinkedToken = _linkedSource.Token;
        
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            var jsonOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = WebsocketError.NonWebsocketRequest;
            response.AddContext(HttpContext);
            // ReSharper disable once MethodSupportsCancellation
            await HttpContext.Response.WriteAsJsonAsync(
                response,
                jsonOptions.Value.SerializerOptions,
                contentType: MediaTypeNames.Application.ProblemJson,
                cancellationToken: cancellationToken);
            return;
        }

        var connectionPrecondition = await ConnectionPrecondition();
        if (connectionPrecondition.IsT1)
        {
            var jsonOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
            var response = connectionPrecondition.AsT1.Value;
            HttpContext.Response.StatusCode = response.Status ?? StatusCodes.Status400BadRequest;
            response.AddContext(HttpContext);
            // ReSharper disable once MethodSupportsCancellation
            await HttpContext.Response.WriteAsJsonAsync(
                response,
                jsonOptions.Value.SerializerOptions,
                contentType: MediaTypeNames.Application.ProblemJson,
                cancellationToken: cancellationToken);
            return;
        }

        Logger.LogInformation("Opening websocket connection");
        
#pragma warning disable IDISP003
        WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
#pragma warning restore IDISP003

#pragma warning disable CS4014
        OsTask.Run(MessageLoop);
#pragma warning restore CS4014

        await SendInitialData();

        await Logic();
        // Logic ended
        
        await UnregisterConnection();

        // Only send close if the socket is still open, this allows us to close the websocket from inside the logic
        // We send close if the client sent a close message though
        if (WebSocket is { State: WebSocketState.Open or WebSocketState.CloseReceived }) 
        {
            try
            {
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal closure",
                    LinkedToken);
            }
            catch (TaskCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
            {
                // Ignore, this happens when the application is shutting down
            }
        }
    }

    #region Send Loop

    /// <summary>
    /// Message loop to send out messages in the channel
    /// </summary>
    [NonAction]
    private async Task MessageLoop()
    {
        await foreach (var msg in Channel.Reader.ReadAllAsync(LinkedToken))
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

    private readonly CancellationTokenSource _receiveCancellationTokenSource = new();
    
    /// <summary>
    /// Main receiver logic for the websocket
    /// </summary>
    /// <returns></returns>
    [NonAction]
    private async Task Logic()
    {
        using var linkedReceiverToken = CancellationTokenSource.CreateLinkedTokenSource(LinkedToken, _receiveCancellationTokenSource.Token);
        
        while (!linkedReceiverToken.IsCancellationRequested)
        {
            try
            {
                if (WebSocket is null)
                {
                    Logger.LogWarning("WebSocket is null, aborting");
                    return;
                }

                if (WebSocket.State is WebSocketState.CloseReceived or WebSocketState.CloseSent
                    or WebSocketState.Closed)
                {
                    // Client or we sent close message or both, we will close the connection after this
                    return;
                }

                if (WebSocket!.State != WebSocketState.Open)
                {
                    Logger.LogWarning("WebSocket is not open [{State}], aborting", WebSocket.State);
                    WebSocket?.Abort();
                    return;
                }

                if (!await HandleReceive(linkedReceiverToken.Token))
                {
                    // HandleReceive returned false, we will close the connection after this
                    Logger.LogDebug("HandleReceive returned false, closing connection");
                    return;
                }

            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // When we dont receive a close message from the client, we will get this exception
                WebSocket?.Abort();
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception while processing websocket request");
                WebSocket?.Abort();
                return;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if you want to continue the receiver loop, false if you want to terminate</returns>
    [NonAction]
    protected abstract Task<bool> HandleReceive(CancellationToken cancellationToken);
    
    [NonAction]
    protected async Task ForceClose(WebSocketCloseStatus closeStatus, string? statusDescription)
    {
        await _receiveCancellationTokenSource.CancelAsync();

        if (WebSocket is { State: WebSocketState.CloseReceived or WebSocketState.Open })
        {
            await WebSocket.CloseOutputAsync(closeStatus, statusDescription, LinkedToken);
        }
    }

    /// <summary>
    /// Send initial data to the client
    /// </summary>
    /// <returns></returns>
    [NonAction]
    protected virtual Task SendInitialData() => Task.CompletedTask;

    /// <summary>
    /// Action when the websocket connection is finished or disposed
    /// </summary>
    [NonAction]
    protected virtual Task UnregisterConnection() => Task.CompletedTask;

    /// <summary>
    /// Action when the websocket connection is destroyed to unregister the connection to a websocket manager
    /// </summary>
    [NonAction]
    protected virtual Task<OneOf<Success, Error<OpenShockProblem>>> ConnectionPrecondition() =>
        Task.FromResult(OneOf<Success, Error<OpenShockProblem>>.FromT0(new Success()));
}