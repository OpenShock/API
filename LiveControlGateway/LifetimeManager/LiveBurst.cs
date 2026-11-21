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

    public LiveBurst(ControlType type, Guid controllerId, DateTime startedAt)
    {
        Type = type;
        ControllerId = controllerId;
        _startedAt = startedAt;
    }

    public ControlType Type { get; }

    public Guid ControllerId { get; }

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
        ControlledByUserId = ControllerId,
        Intensity = (byte)Math.Round((double)_intensityMs / _durationMs),
        MaxIntensity = _maxIntensity,
        Duration = (uint)_durationMs,
        Type = Type,
        CustomName = null,
        LiveControl = true,
        CreatedAt = _startedAt
    };
}
