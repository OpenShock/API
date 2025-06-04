using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class PublicShareSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.PublicShares.Any())
            return;

        var allUserIds = db.Users.Select(u => u.Id).ToList();

        var publicShareFaker = new Faker<PublicShare>()
            .RuleFor(p => p.Id, f => Guid.CreateVersion7())
            .RuleFor(p => p.OwnerId, f => f.PickRandom(allUserIds))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName().Truncate(HardLimits.PublicShareNameMaxLength))
            .RuleFor(p => p.ExpiresAt, f => f.Date.FutureOffset(60).UtcDateTime)
            .RuleFor(p => p.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Roughly one public share per 5 users
        var shares = publicShareFaker.Generate(allUserIds.Count / 5 + 1);
        db.PublicShares.AddRange(shares);
        await db.SaveChangesAsync();
    }
}
