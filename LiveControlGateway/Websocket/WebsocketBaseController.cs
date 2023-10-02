using System.Net.WebSockets;
using System.Threading.Channels;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Utils;
using OpenShock.Serialization;
using OpenShock.ServicesCommon;

namespace OpenShock.LiveControlGateway.Websocket;

public abstract class WebsocketBaseController<T> : OpenShockControllerBase, IWebsocketController<T> where T : class, IFlatBufferSerializable
{
    public abstract Guid Id { get; }
    
    protected readonly ILogger<WebsocketBaseController<T>> Logger;
    protected readonly CancellationTokenSource Close = new();
    protected readonly CancellationTokenSource Linked;
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    protected readonly ISerializer<T> _flatBuffersSerializer;

    protected WebSocket? WebSocket;

    public WebsocketBaseController(ILogger<WebsocketBaseController<T>> logger, IHostApplicationLifetime lifetime, ISerializer<T> flatBuffersSerializer)
    {
        Logger = logger;
        _flatBuffersSerializer = flatBuffersSerializer;
        Linked = CancellationTokenSource.CreateLinkedTokenSource(Close.Token, lifetime.ApplicationStopping);
    }
    
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
    
    [NonAction]
    public async Task MessageLoop()
    {
        await foreach (var msg in _channel.Reader.ReadAllAsync(Linked.Token))
        {
            try
            {
                await WebSocketUtils.SendFullMessage<T>(msg, _flatBuffersSerializer, WebSocket!, Linked.Token);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending message to client");
                throw;
            }
        }
    }

    #endregion
    
    protected abstract Task Logic();
    
    protected virtual void RegisterConnection()
    {
    }
    
    protected virtual void UnregisterConnection()
    {
    }
}