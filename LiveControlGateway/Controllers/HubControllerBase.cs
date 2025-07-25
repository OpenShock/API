﻿using System.Net.WebSockets;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization.Gateway;
using Semver;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Basis for the hub controllers, shares common functionality
/// </summary>
/// <typeparam name="TIn">The type we are receiving / deserializing</typeparam>
/// <typeparam name="TOut">The type we are sending out / serializing</typeparam>
public abstract class HubControllerBase<TIn, TOut> : FlatbuffersWebsocketBaseController<TIn, TOut>, IHubController, IActionFilter where TIn : class, IFlatBufferSerializable where TOut : class, IFlatBufferSerializable
{
    /// <summary>
    /// The current hub
    /// </summary>
    protected Device CurrentHub = null!;
    
    /// <summary>
    /// Service provider
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;
    
    private HubLifetime? _hubLifetime;
    
    /// <summary>
    /// Hub lifetime
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected HubLifetime HubLifetime => _hubLifetime ?? throw new InvalidOperationException("Hub lifetime is null but was tried to access");
    private readonly LcgOptions _options;

    private readonly HubLifetimeManager _hubLifetimeManager;

    private readonly Timer _keepAliveTimeoutTimer = new(Duration.DeviceKeepAliveInitialTimeout);
    private DateTimeOffset _connected = DateTimeOffset.UtcNow;
    private string? _userAgent;

    /// <inheritdoc cref="IHubController.Id" />
    public override Guid Id => CurrentHub.Id;
    
    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        CurrentHub = ControllerContext.HttpContext.RequestServices
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
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    protected HubControllerBase(
        ISerializer<TIn> incomingSerializer,
        ISerializer<TOut> outgoingSerializer,
        HubLifetimeManager hubLifetimeManager,
        IServiceProvider serviceProvider,
        IOptions<LcgOptions> options,
        ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger
        ) : base(logger, incomingSerializer, outgoingSerializer)
    {
        _hubLifetimeManager = hubLifetimeManager;
        ServiceProvider = serviceProvider;
        _options = options.Value;
        _keepAliveTimeoutTimer.Elapsed += async (_, _) =>
        {
            try
            {
                Logger.LogInformation("Keep alive timeout reached, closing websocket connection");
                await ForceClose(WebSocketCloseStatus.ProtocolError, "Keep alive timeout reached");
                WebSocket?.Abort();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while closing websocket connection from keep alive timeout");
            }
        };
        _keepAliveTimeoutTimer.Start();
    }


    private SemVersion? _firmwareVersion;

    /// <inheritdoc />
    protected override async Task<OneOf<Success, Error<OpenShockProblem>>> ConnectionPrecondition()
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
            return err;
        }
        
        _userAgent = HttpContext.Request.Headers.UserAgent.ToString().Truncate(256);
        var hubLifetimeResult = await _hubLifetimeManager.TryAddDeviceConnection(5, this, LinkedToken);

        if (hubLifetimeResult.IsT1)
        {
            Logger.LogWarning("Hub lifetime busy, closing connection");
            return new Error<OpenShockProblem>(WebsocketError.WebsocketHubLifetimeBusy);
        }
        
        if (hubLifetimeResult.IsT2)
        {
            Logger.LogError("Hub lifetime error, closing connection");
            return new Error<OpenShockProblem>(ExceptionError.Exception);
        }
        
        _hubLifetime = hubLifetimeResult.AsT0;
        
        return new Success();
    }
    
    private bool _unregistered;
    
    /// <summary>
    /// When the connection is destroyed
    /// </summary>
    protected override async Task UnregisterConnection()
    {
        if (_unregistered)
            return;
        _unregistered = true;
        
        await _hubLifetimeManager.RemoveDeviceConnection(this);
    }

    /// <inheritdoc />
    public abstract ValueTask Control(List<ShockerCommand> controlCommands);

    /// <inheritdoc />
    public abstract ValueTask CaptivePortal(bool enable);

    /// <inheritdoc />
    public abstract ValueTask OtaInstall(SemVersion version);

    /// <inheritdoc />
    public async Task DisconnectOld()
    {
        if (WebSocket is null)
            return;

        await ForceClose(WebSocketCloseStatus.NormalClosure, "Hub is connecting from a different location");
    }

    private static DateTimeOffset? GetBootedAtFromUptimeMs(ulong uptimeMs)
    {
        var uptime = TimeSpan.FromMilliseconds(uptimeMs);
        if (uptime > HardLimits.FirmwareMaxUptime) return null; // Yeah, ok bro.

        return DateTimeOffset.UtcNow.Subtract(uptime);
    }
    
    /// <summary>
    /// Keep the hub online
    /// </summary>
    protected async Task<bool> SelfOnline(ulong uptimeMs, ushort? latency = null, int? rssi = null)
    {
        var bootedAt = GetBootedAtFromUptimeMs(uptimeMs);
        if (!bootedAt.HasValue)
        {
            Logger.LogDebug("Client attempted to abuse reported boot time, uptime indicated that hub [{HubId}] booted prior to 2024", CurrentHub.Id);
            return false;
        }
        
        Logger.LogDebug("Received keep alive from hub [{HubId}]", CurrentHub.Id);

        // Reset the keep alive timeout
        _keepAliveTimeoutTimer.Interval = Duration.DeviceKeepAliveTimeout.TotalMilliseconds;

        await HubLifetime.Online(CurrentHub.Id, new SelfOnlineData()
        {
            Owner = CurrentHub.OwnerId,
            Gateway = _options.Fqdn,
            FirmwareVersion = _firmwareVersion!,
            ConnectedAt = _connected,
            UserAgent = _userAgent,
            BootedAt = bootedAt.Value,
            LatencyMs = latency,
            Rssi = rssi
        });

        return true;
    }
    
    /// <inheritdoc />
    protected override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}