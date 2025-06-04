using Bogus;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class DeviceSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        // Only seed if there are no devices yet
        if (db.Devices.Any())
            return;

        // Grab all user IDs (including Admin and System) to assign ownership
        var allUserIds = db.Users.Select(u => u.Id).ToList();

        var deviceFaker = new Faker<Device>()
            .RuleFor(d => d.Id, f => Guid.NewGuid())
            .RuleFor(d => d.Name, f => f.Commerce.ProductName().Truncate(HardLimits.HubNameMaxLength))
            .RuleFor(d => d.OwnerId, f => f.PickRandom(allUserIds))
            .RuleFor(d => d.Token, f => f.Random.AlphaNumeric(HardLimits.HubTokenMaxLength))
            .RuleFor(d => d.CreatedAt, f => f.Date.RecentOffset(60).UtcDateTime);

        // Generate 3 fake devices per user
        var bogusDevices = deviceFaker.Generate(allUserIds.Count * 3);
        db.Devices.AddRange(bogusDevices);

        await db.SaveChangesAsync();
    }
}
