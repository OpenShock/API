using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.LiveControlGateway.Metrics;
using OpenShock.LiveControlGateway.Options;
using Redis.OM.Contracts;
using StackExchange.Redis;

using OpenShock.Internal.Common.Extensions;

namespace OpenShock.LiveControlGateway.LifetimeManager;

/// <summary>
/// Lifetime manager for devices, this class is responsible for managing the lifetime of devices
/// </summary>
public sealed class HubLifetimeManager
{
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IRedisPubService _redisPubService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GatewayMetrics _metrics;
    private readonly ILogger<HubLifetimeManager> _logger;
    
    /// <summary>
    /// Immutable so that readers that do not hold <see cref="_lifetimesLock"/> - the observable
    /// instruments below, whose callbacks are synchronous and cannot await it - always see one
    /// consistent version. Mutation is still a read-modify-write of the field, so writers take the
    /// lock, which also keeps the multi-step swap and removal sequences atomic.
    /// </summary>
    private volatile ImmutableDictionary<Guid, HubLifetime> _lifetimes = ImmutableDictionary<Guid, HubLifetime>.Empty;
    private readonly SemaphoreSlim _lifetimesLock = new(1);

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="dbContextFactory"></param>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="redisPubService"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="lcgOptions"></param>
    /// <param name="metrics"></param>
    /// <param name="meter"></param>
    public HubLifetimeManager(
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IConnectionMultiplexer connectionMultiplexer,
        IRedisConnectionProvider redisConnectionProvider,
        IRedisPubService redisPubService,
        ILoggerFactory loggerFactory,
        LcgOptions lcgOptions,
        GatewayMetrics metrics,
        [FromKeyedServices("OpenShock.Gateway.Meter")] Meter meter
    )
    {
        _dbContextFactory = dbContextFactory;
        _connectionMultiplexer = connectionMultiplexer;
        _redisConnectionProvider = redisConnectionProvider;
        _redisPubService = redisPubService;
        _loggerFactory = loggerFactory;
        _metrics = metrics;

        _logger = _loggerFactory.CreateLogger<HubLifetimeManager>();
        
        
        var gatewayFqdn = new KeyValuePair<string, object?>("gateway_fqdn", lcgOptions.Fqdn);

        meter.CreateObservableUpDownCounter("openshock_hub_connections", () =>
        {
            return new[]
            {
                new Measurement<int>(_lifetimes.Count, gatewayFqdn)
            };
        }, "connections", "Current number of connected hubs");

        // Derived from the hubs themselves rather than tracked alongside them: a separately
        // maintained counter drifts from the truth the moment a teardown path misses a decrement.
        meter.CreateObservableUpDownCounter("openshock_live_control_connections", () =>
        {
            var lifetimes = _lifetimes;

            // Enumerate the dictionary itself, not .Values: the former uses ImmutableDictionary's
            // struct enumerator, the latter allocates one.
            var count = 0;
            foreach (var (_, lifetime) in lifetimes) count += lifetime.LiveControlClientCount;

            return new[]
            {
                new Measurement<int>(count, gatewayFqdn)
            };
        }, "connections", "Current number of live control sessions across all connected hubs");
    }

    /// <summary>
    /// When the hub lifetime is busy, we cannot add a new device connection
    /// </summary>
    public readonly struct Busy;

    /// <summary>
    /// Add device to lifetime manager, called on successful connect of device
    /// </summary>
    /// <param name="tps"></param>
    /// <param name="hubController"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<HubLifetime, Busy, Error>> TryAddDeviceConnection(byte tps, IHubController hubController,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding hub lifetime [{HubId}]", hubController.Id);
        var isSwapping = false;
        HubLifetime? hubLifetime;

        using (await _lifetimesLock.LockAsyncScoped(cancellationToken))
        {
            if (_lifetimes.TryGetValue(hubController.Id, out hubLifetime))
            {
                // There already is a hub lifetime, lets swap!
                if (!hubLifetime.TryMarkSwapping())
                {
                    _metrics.HubConnectAttempt(GatewayMetrics.HubConnectOutcome.Busy);
                    return new Busy(); 
                }

                isSwapping = true;
            }
            else
            {
                // This is a fresh connection with no existing lifetime, create one!
                hubLifetime = CreateNewLifetime(tps, hubController);
                _lifetimes = _lifetimes.SetItem(hubController.Id, hubLifetime);
            }
        }


        if (isSwapping)
        {
            _logger.LogTrace("Swapping hub lifetime [{HubId}]", hubController.Id);
            await hubLifetime.Swap(hubController);
            _metrics.HubConnectAttempt(GatewayMetrics.HubConnectOutcome.Swapped);
        }
        else
        {
            _logger.LogTrace("Initializing hub lifetime [{HubId}]", hubController.Id);
            if (!await hubLifetime.InitAsync(cancellationToken))
            {
                // If we fail to initialize, the hub must be removed
                await RemoveDeviceConnection(hubController); // Here be dragons?
                _logger.LogError("Failed to initialize hub lifetime [{HubId}]", hubController.Id);
                _metrics.HubConnectAttempt(GatewayMetrics.HubConnectOutcome.InitFailed);
                return new Error();
            }

            _metrics.HubConnectAttempt(GatewayMetrics.HubConnectOutcome.Connected);
        }

        return hubLifetime;
    }

    private HubLifetime CreateNewLifetime(byte tps, IHubController hubController)
    {
        _logger.LogInformation("New hub connected, creating lifetime [{DeviceId}]", hubController.Id);

        var deviceLifetime = new HubLifetime(
            tps,
            hubController,
            _dbContextFactory,
            _connectionMultiplexer,
            _redisConnectionProvider,
            _redisPubService,
            _loggerFactory.CreateLogger<HubLifetime>());

        return deviceLifetime;
    }

    /// <summary>
    /// Remove device from Lifetime Manager, called on dispose of device controller,
    /// this is the actual end of life of the hub
    /// </summary>
    /// <param name="hubController"></param>
    public async Task RemoveDeviceConnection(IHubController hubController)
    {
        _logger.LogDebug("Removing hub lifetime [{HubId}]", hubController.Id);
        HubLifetime? hubLifetime;
        
        using (await _lifetimesLock.LockAsyncScoped())
        {
            if (!_lifetimes.TryGetValue(hubController.Id, out hubLifetime))
            {
                // its fine, this is also the case when a precondition is not met for example.
                _logger.LogDebug("Hub lifetime not found for hub [{HubId}]", hubController.Id); 
                return;
            }
            
            // Dont remove a hub lifetime that has a different hub controller,
            // this might happen when remove is called after a swap has been fully done
            if(hubLifetime.HubController != hubController) return;

            if (!hubLifetime.TryMarkRemoving())
            {
                return;
            }
        }

        // We need to catch this, we always want to get rid of the hub lifetime even if this failes!
        // Otherwise we end up with a hub not being able to connect again.
        try
        {
            await hubLifetime.DisposeAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown while disposing hub lifetime [{HubId}]", hubController.Id);
        }

        using (await _lifetimesLock.LockAsyncScoped())
        {
            // Remove hands back the same instance when the key was not there, which is the only
            // signal that nothing was removed.
            var withoutHub = _lifetimes.Remove(hubController.Id);
            if (ReferenceEquals(withoutHub, _lifetimes))
            {
                _logger.LogError("Failed to remove hub lifetime [{HubId}], this shouldnt happen WTF?!", hubController.Id);
            }
            else
            {
                _lifetimes = withoutHub;
                _metrics.HubDisconnected();
            }
        }
    }

    /// <summary>
    /// Check if device is connected to LCG
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public bool IsConnected(Guid device) => _lifetimes.ContainsKey(device);

    /// <summary>
    /// Register live control connection to hub lifetime, null if hub not found
    /// </summary>
    /// <param name="liveControlController"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<OneOf.OneOf<HubLifetime, NotFound, Busy>> AddLiveControlConnection(LiveControlController liveControlController)
    {
        if (!liveControlController.HubId.HasValue) throw new ArgumentException("LiveControlController does not have a hubId", nameof(liveControlController));
        
        using (await _lifetimesLock.LockAsyncScoped())
        {
            if (!_lifetimes.TryGetValue(liveControlController.HubId!.Value, out var hubLifetime)) return new NotFound();
            
            if (hubLifetime.State == HubLifetimeState.Removing)
            {
                _logger.LogDebug("Hub lifetime [{HubId}] is removing, cannot add live control connection", liveControlController.HubId);
                return new Busy();
            }
                
            await hubLifetime.AddLiveControlClient(liveControlController);
            return hubLifetime;
        }
    }
    
        /// <summary>
    /// Update device data from the database
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceNotFound>> UpdateDevice(Guid device)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.UpdateDevice();
        return new Success();
    }

    /// <summary>
    /// Control from redis, aka a regular command
    /// </summary>
    /// <param name="device"></param>
    /// <param name="shocks"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceNotFound>> Control(Guid device, IReadOnlyList<ShockerControlCommand> shocks)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.Control(shocks);
        return new Success();
    }

    /// <summary>
    /// Captive portal control from redis
    /// </summary>
    /// <param name="device"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceNotFound>> ControlCaptive(Guid device, bool enabled)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.ControlCaptive(enabled);
        return new Success();
    }

    /// <summary>
    /// Emergency stop from redis, this cannot be undone remotely
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceMissingFeature, DeviceNotFound>> EmergencyStop(Guid device)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        bool ok = await deviceLifetime.EmergencyStop();
        return ok ? new Success() : new DeviceMissingFeature();
    }

    /// <summary>
    /// Ota start install
    /// </summary>
    /// <param name="device"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceNotFound>> OtaInstall(Guid device, SemVersion version)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.OtaInstall(version);
        return new Success();
    }

    /// <summary>
    /// Reboot device
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public async Task<OneOf.OneOf<Success, DeviceMissingFeature, DeviceNotFound>> Reboot(Guid device)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        bool ok = await deviceLifetime.Reboot();
        return ok ? new Success() : new DeviceMissingFeature();
    }

    /// <summary>
    /// Set device online, or update its status
    /// </summary>
    /// <param name="device"></param>
    /// <param name="data"></param>
    public async Task<OneOf.OneOf<Success, DeviceNotFound>> DeviceOnline(Guid device, SelfOnlineData data)
    {
        if (!_lifetimes.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.Online(device, data);
        return new Success();
    }
}

/// <summary>
/// OneOf
/// </summary>
public readonly struct DeviceNotFound;

/// <summary>
/// OneOf
/// </summary>
public readonly record struct ShockerExclusive(DateTimeOffset Until);

/// <summary>
/// This hub is too outdated to use this command
/// </summary>
public readonly struct DeviceMissingFeature;