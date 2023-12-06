using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.ServicesCommon.Websocket
{
    /// <summary>
    /// Base for json serialized websocket controller, you can override the SendMessageMethod to implement a different serializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class WebsocketBaseController<T> : OpenShockControllerBase, IAsyncDisposable, IWebsocketController<T> where T : class
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

        protected WebSocket? WebSocket;

        /// <summary>
        /// DI
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="lifetime"></param>
        public WebsocketBaseController(ILogger<WebsocketBaseController<T>> logger, IHostApplicationLifetime lifetime)
        {
            Logger = logger;
            Linked = CancellationTokenSource.CreateLinkedTokenSource(Close.Token, lifetime.ApplicationStopping);
        }


        /// <inheritdoc />
        public ValueTask QueueMessage(T data) => _channel.Writer.WriteAsync(data);

        private bool _disposed;
    
        /// <inheritdoc />
        [NonAction]
        protected override void Dispose(bool disposing)
        {
            DisposeAsync().AsTask().Wait();
        }
        
        /// <inheritdoc />
        [NonAction]
        public async ValueTask DisposeAsync()
        {
            if(_disposed) return;
            Logger.LogTrace("Disposing websocket controller..");
            _disposed = true;
            await UnregisterConnection();
            
            _channel.Writer.Complete();
            await Close.CancelAsync();
            WebSocket?.Dispose();
            Logger.LogTrace("Disposed websocket controller");
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

            if (!await ConnectionPrecondition())
            { 
                await Close.CancelAsync();
                return;
            }

            Logger.LogInformation("Opening websocket connection");
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await RegisterConnection();

#pragma warning disable CS4014
            LucTask.Run(MessageLoop);
#pragma warning restore CS4014
        
            await Logic();

            await UnregisterConnection();

            await Close.CancelAsync();
        }
    
        #region Send Loop
    
        /// <summary>
        /// Message loop to send out messages in the channel
        /// </summary>
        [NonAction]
        private async Task MessageLoop()
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(Linked.Token))
            {
                try
                {
                    await SendWebSocketMessage(msg, WebSocket!, Linked.Token);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while sending message to client");
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
        /// Action when the websocket connection is created to register the connection to a websocket manager
        /// </summary>
        [NonAction]
        protected virtual Task RegisterConnection() => Task.CompletedTask;

        /// <summary>
        /// Action when the websocket connection is destroyed to unregister the connection to a websocket manager
        /// </summary>
        [NonAction]
        protected virtual Task UnregisterConnection() => Task.CompletedTask;
        
        /// <summary>
        /// Action when the websocket connection is destroyed to unregister the connection to a websocket manager
        /// </summary>
        [NonAction]
        protected virtual Task<bool> ConnectionPrecondition() => Task.FromResult(true);
        
        ~WebsocketBaseController()
        {
            Console.WriteLine("Finalized");
        }
    }
}