﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;
using Redis.OM.Contracts;
using Semver;
using ShockerModelType = OpenShock.Serialization.Types.ShockerModelType;

namespace OpenShock.LiveControlGateway.LifetimeManager;

/// <summary>
/// Handles all Business Logic for a single device
/// </summary>
public sealed class HubLifetime : IAsyncDisposable
{
    private readonly TimeSpan _waitBetweenTicks;
    private readonly ushort _commandDuration;

    private Dictionary<Guid, ShockerState> _shockerStates = new();
    private readonly byte _tps;
    private readonly IHubController _hubController;
    private readonly CancellationToken _cancellationToken;

    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IRedisPubService _redisPubService;

    private readonly ILogger<HubLifetime> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="tps"></param>
    /// <param name="hubController"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="redisPubService"></param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    public HubLifetime([Range(1, 10)] byte tps, IHubController hubController,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IRedisConnectionProvider redisConnectionProvider,
        IRedisPubService redisPubService,
        ILogger<HubLifetime> logger,
        CancellationToken cancellationToken = default)
    {
        _tps = tps;
        _hubController = hubController;
        _cancellationToken = cancellationToken;
        _dbContextFactory = dbContextFactory;
        _redisConnectionProvider = redisConnectionProvider;
        _redisPubService = redisPubService;
        _logger = logger;

        _waitBetweenTicks = TimeSpan.FromMilliseconds(Math.Floor((float)1000 / tps));
        _commandDuration = (ushort)(_waitBetweenTicks.TotalMilliseconds * 2.5);
    }

    /// <summary>
    /// Call on creation to setup shockers for the first time
    /// </summary>
    /// <param name="db"></param>
    public async Task InitAsync(OpenShockContext db)
    {
        await UpdateShockers(db);
#pragma warning disable CS4014
        LucTask.Run(UpdateLoop);
#pragma warning restore CS4014
    }

    private async Task UpdateLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        while (!_cancellationToken.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                await Update();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in Update()");
            }

            var elapsed = stopwatch.Elapsed;
            var waitTime = _waitBetweenTicks - elapsed;
            if (waitTime.TotalMilliseconds < 1)
            {
                _logger.LogWarning("Update loop running behind for device [{DeviceId}]", _hubController.Id);
                continue;
            }

            await Task.Delay(waitTime, _cancellationToken);
        }
    }

    private async Task Update()
    {
        List<ShockerCommand>? commandList = null;
        foreach (var (id, state) in _shockerStates)
        {
            var cur = DateTimeOffset.UtcNow;
            if (state.ActiveUntil < cur || state.ExclusiveUntil >= cur) continue;
            commandList ??= [];

            commandList.Add(new ShockerCommand
            {
                Id = state.RfId,
                Model = (ShockerModelType)state.Model,
                Type = (ShockerCommandType)state.LastType,
                Duration = _commandDuration,
                Intensity = state.LastIntensity
            });
        }

        if (commandList == null) return;

        await _hubController.Control(commandList);
    }

    /// <summary>
    /// Update all shockers config
    /// </summary>
    public async Task UpdateDevice()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(_cancellationToken);
        await UpdateShockers(db);

        foreach (var websocketController in
                 WebsocketManager.LiveControlUsers.GetConnections(_hubController.Id))
            await websocketController.UpdatePermissions(db);
    }

    /// <summary>
    /// Update all shockers config
    /// </summary>
    private async Task UpdateShockers(OpenShockContext db)
    {
        _logger.LogDebug("Updating shockers for device [{DeviceId}]", _hubController.Id);
        var ownShockers = await db.Shockers.Where(x => x.Device == _hubController.Id).Select(x => new ShockerState()
        {
            Id = x.Id,
            Model = x.Model,
            RfId = x.RfId
        }).ToListAsync(cancellationToken: _cancellationToken);
        _shockerStates = ownShockers.ToDictionary(x => x.Id, x => x);
    }

    /// <summary>
    /// Receive a control frame by a client, this implies that limits and permissions have been checked before
    /// </summary>
    /// <param name="shocker"></param>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <param name="tps"></param>
    /// <returns></returns>
    public OneOf<Success, NotFound, ShockerExclusive> ReceiveFrame(Guid shocker, ControlType type, byte intensity,
        byte tps)
    {
        if (!_shockerStates.TryGetValue(shocker, out var state)) return new NotFound();
        if (state.ExclusiveUntil > DateTimeOffset.UtcNow)
            return new ShockerExclusive
            {
                Until = state.ExclusiveUntil
            };

        state.LastType = type;
        state.LastIntensity = intensity;
        state.ActiveUntil = CalculateActiveUntil(tps);
        return new Success();
    }

    private static DateTimeOffset CalculateActiveUntil(byte tps) =>
        DateTimeOffset.UtcNow.AddMilliseconds(Math.Max(1000 / (float)tps * 2.5, 250));

    /// <summary>
    /// Control from redis, aka a regular command
    /// </summary>
    /// <param name="shocks"></param>
    /// <returns></returns>
    public ValueTask Control(IEnumerable<ControlMessage.ShockerControlInfo> shocks)
    {
        var shocksTransformed = new List<ShockerCommand>();
        foreach (var shock in shocks)
        {
            if (!_shockerStates.TryGetValue(shock.Id, out var state)) continue;

            _logger.LogTrace(
                "Control exclusive: {Exclusive}, type: {Type}, duration: {Duration}, intensity: {Intensity}",
                shock.Exclusive, shock.Type, shock.Duration, shock.Intensity);
            state.ExclusiveUntil = shock.Exclusive && shock.Type != ControlType.Stop
                ? DateTimeOffset.UtcNow.AddMilliseconds(shock.Duration)
                : DateTimeOffset.MinValue;

            shocksTransformed.Add(new ShockerCommand
            {
                Id = shock.RfId, Duration = shock.Duration, Intensity = shock.Intensity,
                Type = (ShockerCommandType)shock.Type,
                Model = (ShockerModelType)shock.Model
            });
        }

        return _hubController.Control(shocksTransformed);
    }

    /// <summary>
    /// Control from redis
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public ValueTask ControlCaptive(bool enabled) => _hubController.CaptivePortal(enabled);

    /// <summary>
    /// Ota install from redis
    /// </summary>
    /// <returns></returns>
    public ValueTask OtaInstall(SemVersion semVersion) => _hubController.OtaInstall(semVersion);

    /// <summary>
    /// Update self online status
    /// </summary>
    /// <param name="device"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<OneOf<Success, OnlineStateUpdated>> Online(Guid device, SelfOnlineData data)
    {
        var deviceOnline = _redisConnectionProvider.RedisCollection<DeviceOnline>();
        var deviceId = device.ToString();
        var online = await deviceOnline.FindByIdAsync(deviceId);
        if (online == null)
        {
            await deviceOnline.InsertAsync(new DeviceOnline
            {
                Id = device,
                Owner = data.Owner,
                FirmwareVersion = data.FirmwareVersion,
                Gateway = data.Gateway,
                ConnectedAt = data.ConnectedAt,
                UserAgent = data.UserAgent,
                BootedAt = data.BootedAt,
                LatencyMs = data.LatencyMs,
                Rssi = data.Rssi,
            }, Duration.DeviceKeepAliveTimeout);

            
            await _redisPubService.SendDeviceOnlineStatus(device);
            return new Success();
        }

        // We cannot rely on the json set anymore, since that also happens with uptime and latency
        // as we don't want to send a device online status every time, we will do it here
        online.BootedAt = data.BootedAt;
        online.LatencyMs = data.LatencyMs;
        online.Rssi = data.Rssi;

        var sendOnlineStatusUpdate = false;
        
        if (online.FirmwareVersion != data.FirmwareVersion ||
            online.Gateway != data.Gateway ||
            online.ConnectedAt != data.ConnectedAt ||
            online.UserAgent != data.UserAgent)
        {
            online.Gateway = data.Gateway;
            online.FirmwareVersion = data.FirmwareVersion!;
            online.ConnectedAt = data.ConnectedAt;
            online.UserAgent = data.UserAgent;
            
            sendOnlineStatusUpdate = true;
        }

        await deviceOnline.UpdateAsync(online, Duration.DeviceKeepAliveTimeout);
        
        if (sendOnlineStatusUpdate)
        {
            await _redisPubService.SendDeviceOnlineStatus(device);
            return new OnlineStateUpdated();
        }

        return new Success();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hubController.DisposeAsync();
}

/// <summary>
/// Online state updated
/// </summary>
public readonly struct OnlineStateUpdated;

/// <summary>
/// Self online data struct
/// </summary>
public readonly struct SelfOnlineData
{
    /// <summary>
    /// Man why do I need a constructor for this wtf
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="gateway"></param>
    /// <param name="firmwareVersion"></param>
    /// <param name="connectedAt"></param>
    /// <param name="bootedAt"></param>
    /// <param name="userAgent"></param>
    /// <param name="latencyMs"></param>
    /// <param name="rssi"></param>
    public SelfOnlineData(
        Guid owner,
        string gateway,
        SemVersion firmwareVersion,
        DateTimeOffset connectedAt,
        string userAgent,
        DateTimeOffset bootedAt,
        ushort? latencyMs = null,
        int? rssi = null)
    {
        Owner = owner;
        Gateway = gateway;
        FirmwareVersion = firmwareVersion;
        ConnectedAt = connectedAt;
        UserAgent = userAgent;
        BootedAt = bootedAt;
        LatencyMs = latencyMs;
        Rssi = rssi;
    }
    
    /// <summary>
    /// The owner of the device
    /// </summary>
    public required Guid Owner { get; init; }
    
    /// <summary>
    /// Our gateway fqdn
    /// </summary>
    public required string Gateway { get; init; }
    
    /// <summary>
    /// Firmware version sent by the hub
    /// </summary>
    public required SemVersion FirmwareVersion { get; init; }
    
    /// <summary>
    /// When the websocket connected
    /// </summary>
    public required DateTimeOffset ConnectedAt { get; init; }
    
    /// <summary>
    /// Raw useragent
    /// </summary>
    public string? UserAgent { get; init; } = null;
    
    /// <summary>
    /// Hub uptime
    /// </summary>
    public DateTimeOffset BootedAt { get; init; }
    
    /// <summary>
    /// Measured latency
    /// </summary>
    public ushort? LatencyMs { get; init; } = null;
    
    /// <summary>
    /// Wifi rssi
    /// </summary>
    public int? Rssi { get; init; } = null;
}