using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Helpers;

public static class ControlLogHelper
{
    // Tests run in parallel against one database
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public static async Task<Guid> CreateShocker(WebApplicationFactory factory, Guid ownerId, string name)
    {
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(factory, ownerId);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var shockerId = Guid.CreateVersion7();
        db.Shockers.Add(new Shocker
        {
            Id = shockerId,
            Name = name,
            Model = ShockerModelType.CaiXianlin,
            RfId = 1234,
            DeviceId = deviceId
        });
        await db.SaveChangesAsync();
        return shockerId;
    }

    public static ShockerControlLog Log(Guid ownerId, Guid shockerId, Guid? controllerId, DateTime at,
        byte intensity = 50, uint duration = 1000, ControlType type = ControlType.Shock, bool live = false,
        byte? maxIntensity = null) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerId,
        ShockerId = shockerId,
        ControlledByUserId = controllerId,
        Intensity = intensity,
        MaxIntensity = maxIntensity ?? intensity,
        Duration = duration,
        Type = type,
        CustomName = null,
        LiveControl = live,
        CreatedAt = at
    };

    public static async Task<int> Insert(WebApplicationFactory factory, params ShockerControlLog[] logs)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OpenShockContext>().InsertControlLogsAsync(logs);
    }

    /// <summary>
    /// Logs older than an hour only show up in <c>control_stats</c> once refreshed.
    /// </summary>
    public static async Task RefreshControlStats(WebApplicationFactory factory)
    {
        await RefreshLock.WaitAsync();
        try
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<OpenShockContext>().Database.ExecuteSqlRawAsync(
                "CALL refresh_continuous_aggregate('control_stats', NULL, now() - INTERVAL '1 hour')");
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
