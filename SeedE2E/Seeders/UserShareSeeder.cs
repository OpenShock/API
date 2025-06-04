using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Fakers;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.UserShares.Any())
            return;

        logger.LogInformation("Generating UserShares...");

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();
        var allShockerIds = await db.Shockers.Select(s => s.Id).ToListAsync();

        var shareFaker = new Faker<UserShare>()
            .ApplySafetySettingsRules()
            .RuleFor(s => s.SharedWithUserId, f => f.PickRandom(allUserIds))
            .RuleFor(s => s.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        int sharesCount = allShockerIds.Count / 4 + 1;

        // Generate roughly 1 share per 4 shockers
        var shares = new List<UserShare>(sharesCount);
        for (int i = 0; i < sharesCount; i++)
        {
            var faked = shareFaker.Generate();
            faked.ShockerId = allShockerIds[i];

            shares.Add(faked);
        }

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
