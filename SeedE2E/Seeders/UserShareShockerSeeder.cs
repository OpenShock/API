using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareShockerSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.PublicShareShockerMappings.Any())
            return;

        var allPublicShareIds = db.PublicShares.Select(p => p.Id).ToList();
        var allShockerIds = db.Shockers.Select(s => s.Id).ToList();

        var mappingFaker = new Faker<PublicShareShocker>()
            .RuleFor(m => m.PublicShareId, f => f.PickRandom(allPublicShareIds))
            .RuleFor(m => m.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(m => m.Cooldown, f => f.Random.Int(0, 60000))
            .RuleFor(m => m.AllowShock, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowVibrate, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowSound, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowLiveControl, f => f.Random.Bool(0.2f))
            .RuleFor(m => m.MaxIntensity, f => f.Random.Byte(10, 100))
            .RuleFor(m => m.MaxDuration, f => f.Random.UShort(5000, 30000))
            .RuleFor(m => m.IsPaused, f => f.Random.Bool(0.1f));

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
