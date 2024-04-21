using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.LiveControlGateway.Websocket;
using Semver;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public static class DeviceLifetimeManager
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(DeviceLifetimeManager));
    private static readonly ConcurrentDictionary<Guid, DeviceLifetime> Managers = new();

    /// <summary>
    /// Add device to lifetime manager, called on successful connect of device
    /// </summary>
    /// <param name="tps"></param>
    /// <param name="deviceController"></param>
    /// <param name="db"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DeviceLifetime> AddDeviceConnection(byte tps, DeviceController deviceController,
        OpenShockContext db, IDbContextFactory<OpenShockContext> dbContextFactory, CancellationToken cancellationToken)
    {
            if (Managers.TryGetValue(deviceController.Id, out var oldController))
            {
                Logger.LogDebug("Disposing old device controller");
                await oldController.DisposeAsync();
            }
            Logger.LogInformation("New device connected, creating lifetime [{DeviceId}]", deviceController.Id);
            
            var deviceLifetime = new DeviceLifetime(tps, deviceController, dbContextFactory, cancellationToken);
            await deviceLifetime.InitAsync(db);
            Managers[deviceController.Id] = deviceLifetime;
            
            foreach (var websocketController in WebsocketManager.LiveControlUsers.GetConnections(deviceController.Id)) 
                await websocketController.UpdateConnectedState(true);
            
            return deviceLifetime;
    }

    /// <summary>
    /// Remove device from Lifetime Manager, called on dispose of device controller
    /// </summary>
    /// <param name="deviceController"></param>
    public static async Task RemoveDeviceConnection(DeviceController deviceController)
    {
        foreach (var websocketController in WebsocketManager.LiveControlUsers.GetConnections(deviceController.Id)) 
            await websocketController.UpdateConnectedState(false);
        
        Managers.Remove(deviceController.Id, out _);
    }

    /// <summary>
    /// Check if device is connected to LCG
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public static bool IsConnected(Guid device) => Managers.ContainsKey(device);

    /// <summary>
    /// Receive a control frame by a client, this implies that limits and permissions have been checked before
    /// </summary>
    /// <param name="device"></param>
    /// <param name="shocker"></param>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <returns></returns>
    public static OneOf<Success, DeviceNotFound, ShockerNotFound, ShockerExclusive> ReceiveFrame(Guid device, Guid shocker,
        ControlType type, byte intensity, byte tps)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        var receiveFrameAction = deviceLifetime.ReceiveFrame(shocker, type, intensity, tps);
        if(receiveFrameAction.IsT0) return new Success();
        if(receiveFrameAction.IsT1) return new ShockerNotFound();
        return receiveFrameAction.AsT2;
    }

    /// <summary>
    /// Update device data from the database
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public static async Task<OneOf<Success, DeviceNotFound>> UpdateDevice(Guid device)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.UpdateDevice();
        return new Success();
    }

    /// <summary>
    /// Control from redis, aka a regular command
    /// </summary>
    /// <param name="device"></param>
    /// <param name="shocks"></param>
    /// <returns></returns>
    public static async Task<OneOf<Success, DeviceNotFound>> Control(Guid device, IEnumerable<ControlMessage.ShockerControlInfo> shocks)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.Control(shocks);
        return new Success();
    }

    /// <summary>
    /// Captive portal control from redis
    /// </summary>
    /// <param name="device"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static async Task<OneOf<Success, DeviceNotFound>> ControlCaptive(Guid device, bool enabled)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.ControlCaptive(enabled);
        return new Success();
    }

    /// <summary>
    /// Ota start install
    /// </summary>
    /// <param name="device"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public static async Task<OneOf<Success, DeviceNotFound>> OtaInstall(Guid device, SemVersion version)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.OtaInstall(version);
        return new Success();
    }
}

public struct DeviceNotFound;

public struct ShockerNotFound;

public struct ShockerExclusive
{
    public required DateTimeOffset Until { get; init; }
}