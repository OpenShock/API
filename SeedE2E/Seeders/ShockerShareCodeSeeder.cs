using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class ShockerShareCodeSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.ShockerShareCodes.Any())
            return;

        var allShockerIds = db.Shockers.Select(s => s.Id).ToList();

        var codeFaker = new Faker<ShockerShareCode>()
            .RuleFor(c => c.Id, f => Guid.CreateVersion7())
            .RuleFor(c => c.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(c => c.AllowShock, f => f.Random.Bool(0.8f))
            .RuleFor(c => c.AllowVibrate, f => f.Random.Bool(0.8f))
            .RuleFor(c => c.AllowSound, f => f.Random.Bool(0.8f))
            .RuleFor(c => c.AllowLiveControl, f => f.Random.Bool(0.2f))
            .RuleFor(c => c.MaxIntensity, f => f.Random.Byte(10, 100))
            .RuleFor(c => c.MaxDuration, f => f.Random.UShort(500, 30000))
            .RuleFor(c => c.IsPaused, f => f.Random.Bool(0.1f))
            .RuleFor(c => c.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // One code per shocker
        var codes = codeFaker.Generate(allShockerIds.Count);
        db.ShockerShareCodes.AddRange(codes);
        await db.SaveChangesAsync();
    }
}
