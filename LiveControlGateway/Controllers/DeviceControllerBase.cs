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
using OpenShock.Common.Errors;
using OpenShock.Common.Hubs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Redis;
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
    
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly LCGConfig _lcgConfig;

    private readonly Timer _keepAliveTimeoutTimer = new(Constants.DeviceKeepAliveInitialTimeout);
    private DateTimeOffset _connected = DateTimeOffset.UtcNow;
    private string _userAgent;

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
    
    protected DeviceControllerBase(
        ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger,
        IHostApplicationLifetime lifetime,
        ISerializer<TIn> incomingSerializer,
        ISerializer<TOut> outgoingSerializer,
        IRedisConnectionProvider redisConnectionProvider,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IServiceProvider serviceProvider, LCGConfig lcgConfig
        
        ) : base(logger, lifetime, incomingSerializer, outgoingSerializer)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _dbContextFactory = dbContextFactory;
        ServiceProvider = serviceProvider;
        _lcgConfig = lcgConfig;
        _keepAliveTimeoutTimer.Elapsed += async (sender, args) =>
        {
            Logger.LogInformation("Keep alive timeout reached, closing websocket connection");
            await Close.CancelAsync();
        };
        _keepAliveTimeoutTimer.Start();
    }


    private SemVersion _firmwareVersion;

    /// <inheritdoc />
    protected override async Task<OneOf<Success, Error<OpenShockProblem>>> ConnectionPrecondition()
    {
        _connected = DateTimeOffset.UtcNow;

        if (HttpContext.Request.Headers.TryGetValue("Firmware-Version", out var header) &&
            SemVersion.TryParse(header, SemVersionStyles.Strict, out var version))
        {
            _firmwareVersion = version;
        }
        else return new Error<OpenShockProblem>(WebsocketError.WebsocketHubFirmwareVersionInvalid);
        
        _userAgent = HttpContext.Request.Headers.UserAgent.ToString().Truncate(256);

        return new Success();
    }

    /// <inheritdoc />
    protected override async Task RegisterConnection()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await DeviceLifetimeManager.AddDeviceConnection(5, this, db, _dbContextFactory, Linked.Token);
    }
    
    /// <inheritdoc />
    protected override async Task UnregisterConnection()
    {
        Logger.LogDebug("Unregistering device connection [{DeviceId}]", Id);
        await DeviceLifetimeManager.RemoveDeviceConnection(this);
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
    protected async Task SelfOnline(TimeSpan uptime, TimeSpan? latency = null)
    {
        Logger.LogDebug("Received keep alive from device [{DeviceId}]", CurrentDevice.Id);

        _keepAliveTimeoutTimer.Interval = Constants.DeviceKeepAliveTimeout.TotalMilliseconds;

        var deviceOnline = _redisConnectionProvider.RedisCollection<DeviceOnline>();
        var deviceId = CurrentDevice.Id.ToString();
        var online = await deviceOnline.FindByIdAsync(deviceId);
        if (online == null)
        {
            await deviceOnline.InsertAsync(new DeviceOnline
            {
                Id = CurrentDevice.Id,
                Owner = CurrentDevice.Owner,
                FirmwareVersion = _firmwareVersion,
                Gateway = _lcgConfig.Lcg.Fqdn,
                ConnectedAt = _connected,
                UserAgent = _userAgent
            }, Constants.DeviceKeepAliveTimeout);
            return;
        }

        online.Uptime = uptime;
        online.Latency = latency;
        
        if (online.FirmwareVersion != _firmwareVersion ||
            online.Gateway != _lcgConfig.Lcg.Fqdn ||
            online.ConnectedAt != _connected ||
            online.UserAgent != _userAgent)
        {
            online.Gateway = _lcgConfig.Lcg.Fqdn;
            online.FirmwareVersion = _firmwareVersion;
            online.ConnectedAt = _connected;
            online.UserAgent = _userAgent;
            Logger.LogInformation("Updated details of online device");
        }

        await deviceOnline.UpdateAsync(online, Constants.DeviceKeepAliveTimeout);
    }
    
    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}