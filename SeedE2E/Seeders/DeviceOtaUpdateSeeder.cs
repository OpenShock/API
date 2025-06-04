using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class DeviceOtaUpdateSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.DeviceOtaUpdates.Any())
            return;

        logger.LogInformation("Generating DeviceOtaUpdates...");

        var allDeviceIds = await db.Devices.Select(d => d.Id).ToListAsync();

        var otaFaker = new Faker<DeviceOtaUpdate>()
            .RuleFor(o => o.DeviceId, f => f.PickRandom(allDeviceIds))
            .RuleFor(o => o.UpdateId, f => f.Random.Int())
            .RuleFor(o => o.Version, f => f.System.Semver())
            .RuleFor(o => o.Message, f => f.Rant.Review())
            .RuleFor(o => o.Status, f => f.PickRandom<OtaUpdateStatus>())
            .RuleFor(o => o.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // For each device, generate between 1 and 5 OTA updates
        var allOtaUpdates = new List<DeviceOtaUpdate>();
        foreach (var deviceId in allDeviceIds)
        {
            var count = new Random().Next(1, 6);
            allOtaUpdates.AddRange(otaFaker.Clone().RuleFor(o => o.DeviceId, _ => deviceId).Generate(count));
        }

        db.DeviceOtaUpdates.AddRange(allOtaUpdates);
        await db.SaveChangesAsync();
    }
}
