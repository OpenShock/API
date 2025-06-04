using Bogus;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Fakers;

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
            .ApplySafetySettingsRules()
            .RuleFor(m => m.InviteId, f => f.PickRandom(allInviteIds))
            .RuleFor(m => m.ShockerId, f => f.PickRandom(allShockerIds));

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
