using OpenShock.Common.Models;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public sealed class ShockerManager
{
    public required Guid Id { get; init; }
    public required ushort RfId { get; set; }
    public required ShockerModelType Model { get; set; }

    private Timer _timer = new Timer(Update, null, );

    private void Update(object? state)
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        _timer.
    }
}