using OneOf;
using OneOf.Types;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.LiveControlGateway.Controllers;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public static class DeviceLifetimeManager
{
    private static readonly Dictionary<Guid, DeviceLifetime> Managers = new();
    private static readonly SemaphoreSlim Lock = new(1, 1);

    /// <summary>
    /// Add device to lifetime manager, called on successful connect of device
    /// </summary>
    /// <param name="deviceController"></param>
    /// <param name="db"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DeviceLifetime> AddDeviceConnection(DeviceController deviceController,
        OpenShockContext db, CancellationToken cancellationToken)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            if (Managers.TryGetValue(deviceController.Id, out var oldController))
            {
                await oldController.DisposeAsync();
            }

            var deviceLifetime = new DeviceLifetime(deviceController, cancellationToken);
            await deviceLifetime.InitAsync(db);
            Managers[deviceController.Id] = deviceLifetime;
            return deviceLifetime;
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Remove device from Lifetime Manager, called on dispose of device controller
    /// </summary>
    /// <param name="deviceController"></param>
    public static async Task RemoveDeviceConnection(DeviceController deviceController)
    {
        await Lock.WaitAsync();
        try
        {
            Managers.Remove(deviceController.Id);
        }
        finally
        {
            Lock.Release();
        }
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
}

public struct DeviceNotFound;

public struct ShockerNotFound;