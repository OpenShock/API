using Bogus;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class DeviceOtaUpdateSeeder
{
    private static readonly OtaUpdateStatus[] OtaUpdateStatuses = Enum.GetValues<OtaUpdateStatus>().Cast<OtaUpdateStatus>().ToArray();

    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.DeviceOtaUpdates.Any())
            return;

        var allDeviceIds = db.Devices.Select(d => d.Id).ToList();

        var otaFaker = new Faker<DeviceOtaUpdate>()
            .RuleFor(o => o.DeviceId, f => f.PickRandom(allDeviceIds))
            .RuleFor(o => o.UpdateId, f => f.Random.Int())
            .RuleFor(o => o.Version, f => $"{f.System.Semver()}")
            .RuleFor(o => o.Message, f => f.Lorem.Sentence().Truncate(HardLimits.OtaUpdateMessageMaxLength))
            .RuleFor(o => o.Status, f => f.PickRandom(OtaUpdateStatuses))
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
