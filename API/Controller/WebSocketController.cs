using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShockLink.API.Models.WebSocket;
using ShockLink.API.Utils;
using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Controller;

[ApiController]
[Route("/{version:apiVersion}/ws/device")]
public class WebSocketController : WebsocketControllerBase<ResponseType>
{
    public static WebSocketController Instance;

    public override string Id => "test";
    
    public WebSocketController(ILogger<WebSocketController> logger, IHostApplicationLifetime lifetime) : base(logger, lifetime)
    {
    }

    protected override async Task Logic()
    {
        Instance = this;
        WebSocketReceiveResult? result = null;
        do
        {
            try
            {
                if (WebSocket.State == WebSocketState.Aborted) return;
                var message = await WebSocketUtils.ReceiveFullMessage(WebSocket, Linked.Token);
                result = message.Item1;

                if (result.MessageType == WebSocketMessageType.Close || result.CloseStatus.HasValue)
                {
                    //await SelfOffline();
                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            result.CloseStatusDescription ?? "Normal close", Linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Close.Cancel();
                    Logger.LogInformation("Closing websocket connection");
                    return;
                }

                var msg = Encoding.UTF8.GetString(message.Item2.ToArray());
                var json = JsonConvert.DeserializeObject<BaseRequest>(msg);
                if (json == null) continue;
                await ProcessResult(json);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                Close.Cancel();
                return;
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                    Logger.LogError(e, "Error in receive loop, websocket exception");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception while processing websocket request");
            }
        } while (result is { CloseStatus: null });

        Close.Cancel();
    }
    
        private async Task ProcessResult(BaseRequest json)
    {
        switch (json.RequestType)
        {
            case RequestType.Command:
                //var globalMsg = json.Data?.ToObject<>();
                //if (globalMsg != null) await PubSubManager.GlobalMessage(globalMsg);
                break;
        }
    }

}