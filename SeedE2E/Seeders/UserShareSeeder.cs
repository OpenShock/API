using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

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
            .RuleFor(s => s.SharedWithUserId, f => f.PickRandom(allUserIds))
            .RuleFor(s => s.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(s => s.AllowShock, f => f.Random.Bool(0.7f))
            .RuleFor(s => s.AllowVibrate, f => f.Random.Bool(0.7f))
            .RuleFor(s => s.AllowSound, f => f.Random.Bool(0.7f))
            .RuleFor(s => s.AllowLiveControl, f => f.Random.Bool(0.2f))
            .RuleFor(s => s.MaxIntensity, f => f.Random.Byte(5, 100))
            .RuleFor(s => s.MaxDuration, f => f.Random.UShort(500, 30000))
            .RuleFor(s => s.IsPaused, f => f.Random.Bool(0.1f))
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
