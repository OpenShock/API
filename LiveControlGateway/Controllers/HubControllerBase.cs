﻿using System.Net.WebSockets;
using FlatSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common;
using OpenShock.Common.Authentication;
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
    
    private HubLifetime? _hubLifetime = null;
    
    /// <summary>
    /// Hub lifetime
    /// </summary>
    /// <exception cref="Exception"></exception>
    protected HubLifetime HubLifetime => _hubLifetime ?? throw new Exception("Hub lifetime is null but was tried to access");

    private readonly LCGConfig _lcgConfig;

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
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="lcgConfig"></param>
    protected HubControllerBase(
        ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger,
        IHostApplicationLifetime lifetime,
        ISerializer<TIn> incomingSerializer,
        ISerializer<TOut> outgoingSerializer,
        HubLifetimeManager hubLifetimeManager,
        IServiceProvider serviceProvider,
        LCGConfig lcgConfig
        ) : base(logger, lifetime, incomingSerializer, outgoingSerializer)
    {
        _hubLifetimeManager = hubLifetimeManager;
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
    
    
    /// <summary>
    /// Register to the hub lifetime manager
    /// </summary>
    /// <returns></returns>
    protected override async Task<bool> TryRegisterConnection()
    {
        _hubLifetime = await _hubLifetimeManager.TryAddDeviceConnection(5, this, LinkedToken);
        return _hubLifetime != null;
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


    /// <summary>
    /// Keep the hub online
    /// </summary>
    protected async Task SelfOnline(DateTimeOffset bootedAt, ushort? latency = null, int? rssi = null)
    {
        Logger.LogDebug("Received keep alive from hub [{HubId}]", CurrentHub.Id);

        // Reset the keep alive timeout
        _keepAliveTimeoutTimer.Interval = Duration.DeviceKeepAliveTimeout.TotalMilliseconds;

        await HubLifetime.Online(CurrentHub.Id, new SelfOnlineData()
        {
            Owner = CurrentHub.Owner,
            Gateway = _lcgConfig.Lcg.Fqdn,
            FirmwareVersion = _firmwareVersion!,
            ConnectedAt = _connected,
            UserAgent = _userAgent,
            BootedAt = bootedAt,
            LatencyMs = latency,
            Rssi = rssi
        });
    }
    
    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}