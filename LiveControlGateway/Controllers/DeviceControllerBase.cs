using System.Net.WebSockets;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Hubs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization.Gateway;
using Redis.OM.Contracts;
using Semver;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Basis for the device controllers, shares common functionality
/// </summary>
/// <typeparam name="TIn">The type we are receiving / deserializing</typeparam>
/// <typeparam name="TOut">The type we are sending out / serializing</typeparam>
public abstract class DeviceControllerBase<TIn, TOut> : FlatbuffersWebsocketBaseController<TIn, TOut>, IDeviceController, IActionFilter where TIn : class, IFlatBufferSerializable where TOut : class, IFlatBufferSerializable
{
    /// <summary>
    /// The current device
    /// </summary>
    protected Device CurrentDevice = null!;
    
    /// <summary>
    /// Service provider
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    private readonly LCGConfig _lcgConfig;

    private readonly DeviceLifetimeManager _deviceLifetimeManager;

    private readonly Timer _keepAliveTimeoutTimer = new(Duration.DeviceKeepAliveInitialTimeout);
    private DateTimeOffset _connected = DateTimeOffset.UtcNow;
    private string? _userAgent;

    /// <inheritdoc cref="IDeviceController.Id" />
    public override Guid Id => CurrentDevice.Id;
    
    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentDevice = ControllerContext.HttpContext.RequestServices
            .GetRequiredService<IClientAuthService<Device>>()
            .CurrentClient;
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <summary>
    /// Base for hub websocket controllers
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    /// <param name="deviceLifetimeManager"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="lcgConfig"></param>
    protected DeviceControllerBase(
        ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger,
        IHostApplicationLifetime lifetime,
        ISerializer<TIn> incomingSerializer,
        ISerializer<TOut> outgoingSerializer,
        DeviceLifetimeManager deviceLifetimeManager,
        IServiceProvider serviceProvider,
        LCGConfig lcgConfig
        ) : base(logger, lifetime, incomingSerializer, outgoingSerializer)
    {
        _deviceLifetimeManager = deviceLifetimeManager;
        ServiceProvider = serviceProvider;
        _lcgConfig = lcgConfig;
        _keepAliveTimeoutTimer.Elapsed += async (sender, args) =>
        {
            Logger.LogInformation("Keep alive timeout reached, closing websocket connection");
            await Close.CancelAsync();
        };
        _keepAliveTimeoutTimer.Start();
    }


    private SemVersion? _firmwareVersion;

    /// <inheritdoc />
    protected override Task<OneOf<Success, Error<OpenShockProblem>>> ConnectionPrecondition()
    {
        _connected = DateTimeOffset.UtcNow;

        if (HttpContext.Request.Headers.TryGetValue("Firmware-Version", out var header) &&
            SemVersion.TryParse(header, SemVersionStyles.Strict, out var version))
        {
            _firmwareVersion = version;
        }
        else
        {
            var err = new Error<OpenShockProblem>(WebsocketError.WebsocketHubFirmwareVersionInvalid);
            return Task.FromResult(OneOf<Success, Error<OpenShockProblem>>.FromT1(err));
        }
        
        _userAgent = HttpContext.Request.Headers.UserAgent.ToString().Truncate(256);

        return Task.FromResult(OneOf<Success, Error<OpenShockProblem>>.FromT0(new Success()));
    }

    /// <inheritdoc />
    protected override async Task RegisterConnection()
    {
        await _deviceLifetimeManager.AddDeviceConnection(5, this, LinkedToken);
    }
    
    /// <inheritdoc />
    protected override async Task UnregisterConnection()
    {
        Logger.LogDebug("Unregistering device connection [{DeviceId}]", Id);
        await _deviceLifetimeManager.RemoveDeviceConnection(this);
    }
    
    /// <inheritdoc />
    public abstract ValueTask Control(List<ShockerCommand> controlCommands);

    /// <inheritdoc />
    public abstract ValueTask CaptivePortal(bool enable);

    /// <inheritdoc />
    public abstract ValueTask OtaInstall(SemVersion version);
    
    /// <summary>
    /// Keep the device online
    /// </summary>
    protected async Task SelfOnline(DateTimeOffset bootedAt, ushort? latency = null, int? rssi = null)
    {
        Logger.LogDebug("Received keep alive from device [{DeviceId}]", CurrentDevice.Id);

        // Reset the keep alive timeout
        _keepAliveTimeoutTimer.Interval = Duration.DeviceKeepAliveTimeout.TotalMilliseconds;

        var result = await _deviceLifetimeManager.DeviceOnline(CurrentDevice.Id, new SelfOnlineData()
        {
            Owner = CurrentDevice.Owner,
            Gateway = _lcgConfig.Lcg.Fqdn,
            FirmwareVersion = _firmwareVersion!,
            ConnectedAt = _connected,
            UserAgent = _userAgent,
            BootedAt = bootedAt,
            LatencyMs = latency,
            Rssi = rssi
        });
        
        if (result.IsT1)
        {
            Logger.LogError("Error while updating device online status [{DeviceId}], we dont exist in the managers list", CurrentDevice.Id);
            await Close.CancelAsync();
            if (WebSocket != null)
                await WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Device not found in manager",
                    CancellationToken.None);
        }
    }
    
    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}