using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.Serialization;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public sealed class DeviceLifetimeManager : IAsyncDisposable
{
    private static readonly Dictionary<Guid, DeviceLifetimeManager> Managers = new();
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly DeviceController _deviceController;

    private DeviceLifetimeManager(DeviceController deviceController)
    {
        _deviceController = deviceController;
    }

    public static async Task AddDeviceConnection(DeviceController deviceController, CancellationToken cancellationToken)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            if (Managers.TryGetValue(deviceController.Id, out var oldController))
            {
                await oldController.DisposeAsync();
            }

            Managers[deviceController.Id] = new DeviceLifetimeManager(deviceController);
        }
        finally
        {
            Lock.Release();
        }
    }

    public static async Task RemoveDeviceConnection(DeviceController deviceController, CancellationToken cancellationToken)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            Managers.Remove(deviceController.Id);
        }
        finally
        {
            Lock.Release();
        }
    }

    public static bool IsConnected(Guid id) => Managers.ContainsKey(id);

    
    private DateTimeOffset _lastPacket = DateTimeOffset.MinValue;
    private byte _lastIntensity = 0;
    private ControlType _lastType;

    /// <summary>
    /// Update all shockers config
    /// </summary>
    private async Task UpdateShockers(OpenShockContext db)
    {
        db.Shockers.Where(x => x.)
    }
    
    public async Task ReceivePacket(Guid shockerId, byte intensity)
    {
        await _deviceController.QueueMessage(new ServerToDeviceMessage
        {
            Payload = new ServerToDeviceMessagePayload(new ShockerCommandList
            {
                Commands = new List<ShockerCommand>
                {
                    new ShockerCommand
                    {
                    }
                }
            })
        });
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _deviceController.DisposeAsync();
    }
}