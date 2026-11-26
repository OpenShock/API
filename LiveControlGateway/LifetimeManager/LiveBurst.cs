using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.LiveControlGateway.LifetimeManager;

/// <summary>
/// A continuous run of live control with the same type and controller, logged as one <see cref="ShockerControlLog"/>.
/// </summary>
public sealed class LiveBurst
{
    private readonly Guid _id = Guid.CreateVersion7();
    private readonly DateTime _startedAt;
    private long _durationMs;
    private long _intensityMs;
    private byte _maxIntensity;

    public LiveBurst(ControlType type, ControlLogSender controller, DateTime startedAt)
    {
        Type = type;
        Controller = controller;
        _startedAt = startedAt;
    }

    public ControlType Type { get; }

    public ControlLogSender Controller { get; }

    public void AddTick(byte intensity, long tickMs)
    {
        _durationMs += tickMs;
        _intensityMs += intensity * tickMs;
        _maxIntensity = Math.Max(_maxIntensity, intensity);
    }

    public ShockerControlLog ToLog(ShockerState shocker) => new()
    {
        Id = _id,
        OwnerUserId = shocker.OwnerId,
        ShockerId = shocker.Id,
        ControlledByUserId = Controller.Id,
        Intensity = (byte)Math.Round((double)_intensityMs / _durationMs),
        MaxIntensity = _maxIntensity,
        Duration = (uint)_durationMs,
        Type = Type,
        CustomName = null,
        LiveControl = true,
        CreatedAt = _startedAt
    };

    public static ControlLog ToHubLog(ShockerState shocker, ShockerControlLog log) => new()
    {
        Shocker = new BasicShockerInfo { Id = shocker.Id, Name = shocker.Name },
        Type = log.Type,
        Intensity = log.Intensity,
        Duration = log.Duration,
        ExecutedAt = log.CreatedAt,
        LiveControl = true
    };
}
