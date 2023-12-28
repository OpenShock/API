using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.LiveControlGateway.Websocket;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public static class DeviceLifetimeManager
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(DeviceLifetimeManager));
    private static readonly ConcurrentDictionary<Guid, DeviceLifetime> Managers = new();

    /// <summary>
    /// Add device to lifetime manager, called on successful connect of device
    /// </summary>
    /// <param name="deviceController"></param>
    /// <param name="db"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DeviceLifetime> AddDeviceConnection(DeviceController deviceController,
        OpenShockContext db, IDbContextFactory<OpenShockContext> dbContextFactory, CancellationToken cancellationToken)
    {
            if (Managers.TryGetValue(deviceController.Id, out var oldController))
            {
                Logger.LogDebug("Disposing old device controller");
                await oldController.DisposeAsync();
            }
            Logger.LogInformation("New device connected, creating lifetime [{DeviceId}]", deviceController.Id);
            
            var deviceLifetime = new DeviceLifetime(deviceController, dbContextFactory, cancellationToken);
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
    public static OneOf<Success, DeviceNotFound, ShockerNotFound> ReceiveFrame(Guid device, Guid shocker,
        ControlType type, byte intensity)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        return deviceLifetime.ReceiveFrame(shocker, type, intensity) ? new Success() : new ShockerNotFound();
    }

    public static async Task<OneOf<Success, DeviceNotFound>> UpdateDevice(Guid device)
    {
        if (!Managers.TryGetValue(device, out var deviceLifetime)) return new DeviceNotFound();
        await deviceLifetime.UpdateDevice();
        return new Success();
    }
}

public struct DeviceNotFound;

public struct ShockerNotFound;