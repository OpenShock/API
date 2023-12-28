using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.Serialization.Types;
using ShockerModelType = OpenShock.Serialization.Types.ShockerModelType;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public sealed class DeviceLifetime : IAsyncDisposable
{
    private static readonly TimeSpan WaitBetweenTicks = TimeSpan.FromMilliseconds(100); // 10 TPS
    private static readonly TimeSpan AcceptanceStateAge = TimeSpan.FromMilliseconds(200);
    private const ushort CommandDuration = 250;
    private static readonly ILogger<DeviceLifetime> Logger = ApplicationLogging.CreateLogger<DeviceLifetime>();
    
    private Dictionary<Guid, ShockerState> _shockerStates = new();
    private readonly DeviceController _deviceController;
    private readonly CancellationToken _cancellationToken;
    
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;

    public DeviceLifetime(DeviceController deviceController, IDbContextFactory<OpenShockContext> dbContextFactory, CancellationToken cancellationToken = default)
    {
        _deviceController = deviceController;
        _cancellationToken = cancellationToken;
        _dbContextFactory = dbContextFactory;
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
                Logger.LogError(e, "Error in Update()");
            }
            
            var elapsed = stopwatch.Elapsed;
            var waitTime = WaitBetweenTicks - elapsed;
            if (waitTime.TotalMilliseconds < 1)
            {
                Logger.LogWarning("Update loop running behind for device [{DeviceId}]", _deviceController.Id);
                continue;
            }

            await Task.Delay(waitTime, _cancellationToken);
        }
    }
    
    private async Task Update()
    {
        var acceptedTimestamp = DateTimeOffset.UtcNow.Subtract(AcceptanceStateAge);
        List<ShockerCommand>? commandList = null;
        foreach (var (id, state) in _shockerStates)
        {
            if (state.LastReceive < acceptedTimestamp) continue;
            commandList ??= [];
            
            commandList.Add(new ShockerCommand
            {
                Id = state.RfId,
                Model = (ShockerModelType)state.Model,
                Type = (ShockerCommandType)state.LastType,
                Duration = CommandDuration,
                Intensity = state.LastIntensity
            });
        }

        if(commandList == null) return;
        
        await _deviceController.QueueMessage(new ServerToDeviceMessage
        {
            Payload = new ServerToDeviceMessagePayload(new ShockerCommandList
            {
                Commands = commandList
            })
        });
    }

    /// <summary>
    /// Update all shockers config
    /// </summary>
    public async Task UpdateDevice()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(_cancellationToken);
        await UpdateShockers(db);
        
        foreach (var websocketController in WebsocketManager.LiveControlUsers.GetConnections(_deviceController.Id)) 
            await websocketController.UpdatePermissions(db);
    }

    /// <summary>
    /// Update all shockers config
    /// </summary>
    private async Task UpdateShockers(OpenShockContext db)
    {
        Logger.LogDebug("Updating shockers for device [{DeviceId}]", _deviceController.Id);
        var ownShockers = await db.Shockers.Where(x => x.Device == _deviceController.Id).Select(x => new ShockerState()
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
    /// <returns></returns>
    public bool ReceiveFrame(Guid shocker, ControlType type, byte intensity)
    {
        if (!_shockerStates.TryGetValue(shocker, out var state)) return false;

        state.LastType = type;
        state.LastIntensity = intensity;
        state.LastReceive = DateTimeOffset.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _deviceController.DisposeAsync();
    }
}