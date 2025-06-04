using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Fakers;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareShockerSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.PublicShareShockerMappings.Any())
            return;

        var allPublicShareIds = await db.PublicShares.Select(p => p.Id).ToListAsync();
        var allShockerIds = await db.Shockers.Select(s => s.Id).ToListAsync();

        var mappingFaker = new Faker<PublicShareShocker>()
            .ApplySafetySettingsRules()
            .RuleFor(m => m.PublicShareId, f => f.PickRandom(allPublicShareIds))
            .RuleFor(m => m.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(m => m.Cooldown, f => f.Random.Int(0, 60000));

        // Roughly 2 mappings per public share
        var mappings = mappingFaker.Generate(allPublicShareIds.Count * 2);
        // Remove duplicates
        mappings = mappings
            .GroupBy(x => (x.PublicShareId, x.ShockerId))
            .Select(g => g.First())
            .ToList();

        db.PublicShareShockerMappings.AddRange(mappings);
        await db.SaveChangesAsync();
    }
}
