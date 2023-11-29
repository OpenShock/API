using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Utils;
using OpenShock.ServicesCommon.Websocket;

namespace OpenShock.LiveControlGateway.Controllers;

[ApiController]
[Route("/{version:apiVersion}/ws/live/{deviceId:guid}")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo)]
public sealed class LiveControlController : WebsocketBaseController<IBaseResponse<LiveResponseType>>
{
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly OpenShockContext _db;
    
    private LinkUser _currentUser;
    private Guid? _deviceId;
    
    public LiveControlController(OpenShockContext db, ILogger<WebsocketBaseController<IBaseResponse<LiveResponseType>>> logger, IHostApplicationLifetime lifetime, IDbContextFactory<OpenShockContext> dbContextFactory) : base(logger, lifetime)
    {
        _dbContextFactory = dbContextFactory;
        _db = db;
    }

    protected override async Task<bool> ConnectionPrecondition()
    {
        if (HttpContext.GetRouteValue("deviceId") is not string param || !Guid.TryParse(param, out var id))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return false;
        }
        
        _deviceId = id;

        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == _deviceId && (x.Owner == _currentUser.DbUser.Id || x.Shockers.Any(y => y.ShockerShares.Any(
                z => z.SharedWith == _currentUser.DbUser.Id))));

        HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        
        return deviceExistsAndYouHaveAccess;
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>().CurrentClient;
        base.OnActionExecuting(context);
    }

    /// <inheritdoc />
    public override Guid Id => _deviceId ?? throw new Exception("Device id is null");


    /// <inheritdoc />
    protected override async Task Logic()
    {
        while(true)
        {
            try
            {
                if (WebSocket!.State == WebSocketState.Aborted) return;
                var message =
                    await JsonWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseRequest<LiveRequestType>>(WebSocket,
                        Linked.Token);

                if (message.IsT2)
                {
                    if (WebSocket.State != WebSocketState.Open)
                    {
                        Logger.LogWarning("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            Linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Logger.LogInformation("Closing websocket connection");
                    break;
                }
                
                message.Switch(wsRequest =>
                    {
                        if (wsRequest?.Data == null) return;
                        Task.Run(() => ProcessResult(wsRequest));
                    },
                    failed => { Logger.LogWarning(failed.Exception, "Deserialization failed for websocket message"); },
                    _ => { });
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                break;
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
        }

        await Close.CancelAsync();
    }

    private Task ProcessResult(BaseRequest<LiveRequestType> request)
    {
        
    }
}