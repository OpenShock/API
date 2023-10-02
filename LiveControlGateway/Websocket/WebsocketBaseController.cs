using System.Net.WebSockets;
using System.Threading.Channels;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Utils;
using OpenShock.Serialization;
using OpenShock.ServicesCommon;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Base for a flat buffers serialized websocket controller
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class WebsocketBaseController<T> : OpenShockControllerBase, IWebsocketController<T> where T : class, IFlatBufferSerializable
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
    protected readonly CancellationTokenSource Linked;
    
    /// <summary>
    /// Channel for multithreading thread safety of the websocket, MessageLoop is the only reader for this channel
    /// </summary>
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    protected readonly ISerializer<T> _flatBuffersSerializer;

    protected WebSocket? WebSocket;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="flatBuffersSerializer"></param>
    public WebsocketBaseController(ILogger<WebsocketBaseController<T>> logger, IHostApplicationLifetime lifetime, ISerializer<T> flatBuffersSerializer)
    {
        Logger = logger;
        _flatBuffersSerializer = flatBuffersSerializer;
        Linked = CancellationTokenSource.CreateLinkedTokenSource(Close.Token, lifetime.ApplicationStopping);
    }


    /// <inheritdoc />
    public ValueTask QueueMessage(T data) => _channel.Writer.WriteAsync(data);

    /// <inheritdoc />
    [NonAction]
    protected override void Dispose(bool disposing)
    {
        UnregisterConnection();

        _channel.Writer.Complete();
        Close.Cancel();
        WebSocket?.Dispose();
    }
    
    /// <summary>
    /// Initial get request to the websocket route - rewrite to websocket connection
    /// </summary>
    [HttpGet]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Logger.LogInformation("Opening websocket connection");
        WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        RegisterConnection();

#pragma warning disable CS4014
        LucTask.Run(MessageLoop);
#pragma warning restore CS4014
        
        await Logic();

        UnregisterConnection();

        Close.Cancel();
    }
    
    #region Send Loop
    
    /// <summary>
    /// Message loop to send out messages in the channel
    /// </summary>
    [NonAction]
    public async Task MessageLoop()
    {
        await foreach (var msg in _channel.Reader.ReadAllAsync(Linked.Token))
        {
            try
            {
                await WebSocketUtils.SendFullMessage(msg, _flatBuffersSerializer, WebSocket!, Linked.Token);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending message to client");
                throw;
            }
        }
    }

    #endregion
    
    /// <summary>
    /// Main receiver logic for the websocket
    /// </summary>
    /// <returns></returns>
    protected abstract Task Logic();
    
    /// <summary>
    /// Action when the websocket connection is created to register the connection to a websocket manager
    /// </summary>
    protected virtual void RegisterConnection()
    {
    }
    
    /// <summary>
    /// Action when the websocket connection is destroyed to unregister the connection to a websocket manager
    /// </summary>
    protected virtual void UnregisterConnection()
    {
    }
}