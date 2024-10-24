using System.Net.WebSockets;
using FlatSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Hubs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
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
    

    private SemVersion? FirmwareVersion { get; set; }
    
    /// <inheritdoc />
    protected override async Task RegisterConnection()
    {
        _connected = DateTimeOffset.UtcNow;
        
        if (HttpContext.Request.Headers.TryGetValue("Firmware-Version", out var header) &&
            SemVersion.TryParse(header, SemVersionStyles.Strict, out var version)) FirmwareVersion = version;

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
    protected async Task SelfOnline()
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
                FirmwareVersion = FirmwareVersion,
                Gateway = _lcgConfig.Lcg.Fqdn,
                ConnectedAt = _connected
            }, Constants.DeviceKeepAliveTimeout);
            return;
        }

        if (online.FirmwareVersion != FirmwareVersion || online.Gateway != _lcgConfig.Lcg.Fqdn ||
            online.ConnectedAt != _connected)
        {
            online.Gateway = _lcgConfig.Lcg.Fqdn;
            online.FirmwareVersion = FirmwareVersion;
            online.ConnectedAt = _connected;
            await deviceOnline.SaveAsync();
            Logger.LogInformation("Updated details of online device");
        }

        await _redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(DeviceOnline).FullName}:{CurrentDevice.Id}", Constants.DeviceKeepAliveTimeoutIntBoxed);
    }
    
    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimeoutTimer.Dispose();
        return base.DisposeControllerAsync();
    }

}