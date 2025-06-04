using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class UserDeactivationSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.UserDeactivations.Any())
            return;

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();
        var possibleModerators = allUserIds.ToList();

        var deactivationFaker = new Faker<UserDeactivation>()
            .RuleFor(d => d.DeactivatedUserId, f => f.PickRandom(allUserIds))
            .RuleFor(d => d.DeactivatedByUserId, (f, d) =>
            {
                var moderator = f.PickRandom(possibleModerators.Where(x => x != d.DeactivatedUserId));
                return moderator;
            })
            .RuleFor(d => d.DeleteLater, f => f.Random.Bool(0.2f))
            .RuleFor(d => d.UserModerationId, f => f.Random.Uuid()) // placeholder moderation ID
            .RuleFor(d => d.CreatedAt, f => f.Date.RecentOffset(60).UtcDateTime);

        // Roughly deactivating 10% of users
        var count = (int)(allUserIds.Count * 0.1);
        var deactivations = deactivationFaker.Generate(count);
        // Ensure no duplicate DeactivatedUserId
        deactivations = deactivations
            .GroupBy(x => x.DeactivatedUserId)
            .Select(g => g.First())
            .ToList();

        db.UserDeactivations.AddRange(deactivations);
        await db.SaveChangesAsync();
    }
}
