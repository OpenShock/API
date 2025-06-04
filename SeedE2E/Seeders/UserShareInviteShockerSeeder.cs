using Bogus;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareInviteShockerSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.UserShareInviteShockers.Any())
            return;

        var allInviteIds = db.UserShareInvites.Select(i => i.Id).ToList();
        var allShockerIds = db.Shockers.Select(s => s.Id).ToList();

        var mappingFaker = new Faker<UserShareInviteShocker>()
            .RuleFor(m => m.InviteId, f => f.PickRandom(allInviteIds))
            .RuleFor(m => m.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(m => m.AllowShock, f => f.Random.Bool(0.8f))
            .RuleFor(m => m.AllowVibrate, f => f.Random.Bool(0.8f))
            .RuleFor(m => m.AllowSound, f => f.Random.Bool(0.8f))
            .RuleFor(m => m.AllowLiveControl, f => f.Random.Bool(0.3f))
            .RuleFor(m => m.MaxIntensity, f => f.Random.Byte(10, 100))
            .RuleFor(m => m.MaxDuration, f => f.Random.UShort(500, 30000))
            .RuleFor(m => m.IsPaused, f => f.Random.Bool(0.1f));

        // Roughly 2 mappings per invite
        var mappings = mappingFaker.Generate(allInviteIds.Count * 2);
        // Ensure uniqueness per (InviteId, ShockerId)
        mappings = mappings
            .GroupBy(x => (x.InviteId, x.ShockerId))
            .Select(g => g.First())
            .ToList();

        db.UserShareInviteShockers.AddRange(mappings);
        await db.SaveChangesAsync();
    }
}
