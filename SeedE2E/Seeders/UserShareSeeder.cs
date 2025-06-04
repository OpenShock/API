using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Fakers;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.UserShares.Any())
            return;

        var allUserIds = db.Users.Select(u => u.Id).ToList();
        var allShockerIds = db.Shockers.Select(s => s.Id).ToList();

        var shareFaker = new Faker<UserShare>()
            .ApplySafetySettingsRules()
            .RuleFor(s => s.SharedWithUserId, f => f.PickRandom(allUserIds))
            .RuleFor(s => s.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(s => s.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Generate roughly 1 share per 4 shockers
        var shares = shareFaker.Generate(allShockerIds.Count / 4 + 1);
        // Exclude shares where user == owner of shocker via Device→Shocker→Device→Owner
        var validShares = new List<UserShare>();
        foreach (var share in shares)
        {
            var shocker = await db.Shockers.Include(s => s.Device).FirstOrDefaultAsync(s => s.Id == share.ShockerId);
            if (shocker != null && shocker.Device.OwnerId != share.SharedWithUserId)
                validShares.Add(share);
        }

        db.UserShares.AddRange(validShares);
        await db.SaveChangesAsync();
    }
}
