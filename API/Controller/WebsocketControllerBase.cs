using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Utils;
using OpenShock.Common.Models.WebSocket;
using OpenShock.ServicesCommon;

namespace OpenShock.API.Controller;

public abstract class WebsocketControllerBase<T> : ShockLinkControllerBase, IWebsocketController<T> where T : Enum
{
    protected readonly ILogger<WebsocketControllerBase<T>> Logger;
    protected readonly CancellationTokenSource Close = new();
    protected readonly CancellationTokenSource Linked;
    private readonly Channel<IBaseResponse<T>> _channel = Channel.CreateUnbounded<IBaseResponse<T>>();
    protected WebSocket WebSocket = null!;

    public WebsocketControllerBase(ILogger<WebsocketControllerBase<T>> logger, IHostApplicationLifetime lifetime)
    {
        Logger = logger;
        Linked = CancellationTokenSource.CreateLinkedTokenSource(Close.Token, lifetime.ApplicationStopping);
    }

    public abstract Guid Id { get; }
    [NonAction]
    public ValueTask QueueMessage(IBaseResponse<T> data) => _channel.Writer.WriteAsync(data);

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
        try
        {
            LucTask.Run(SendInitialData);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while sending initial data");
        }
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
                await WebSocketUtils.SendFullMessage(msg, WebSocket, Linked.Token);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending message to client");
                throw;
            }
        }
    }

    #endregion

    protected virtual Task SendInitialData() => Task.CompletedTask;
    

    protected abstract Task Logic();
    protected virtual void RegisterConnection()
    {
    }
    
    protected virtual void UnregisterConnection()
    {
    }
}