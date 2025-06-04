using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Fakers;

namespace OpenShock.SeedE2E.Seeders;

public static class ShockerShareCodeSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.ShockerShareCodes.Any())
            return;

        var allShockerIds = db.Shockers.Select(s => s.Id).ToList();

        var codeFaker = new Faker<ShockerShareCode>()
            .ApplySafetySettingsRules()
            .RuleFor(c => c.Id, f => Guid.CreateVersion7())
            .RuleFor(c => c.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(c => c.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // One code per shocker
        var codes = codeFaker.Generate(allShockerIds.Count);
        db.ShockerShareCodes.AddRange(codes);
        await db.SaveChangesAsync();
    }
}
