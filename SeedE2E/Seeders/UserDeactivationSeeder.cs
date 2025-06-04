using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class UserDeactivationSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.UserDeactivations.Any())
            return;

        logger.LogInformation("Generating UserDeactivations...");

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();
        var moderatorIds = await db.Users.Where(u => u.Roles.Any(r => r == RoleType.Staff || r == RoleType.Admin)).Select(m => m.Id).ToListAsync();

        var deactivationFaker = new Faker<UserDeactivation>()
            .RuleFor(d => d.DeactivatedUserId, f => f.PickRandom(allUserIds))
            .RuleFor(d => d.DeleteLater, f => f.Random.Bool(0.2f))
            .RuleFor(d => d.UserModerationId, f => f.Random.Bool(0.1f) ? f.Random.Uuid() : null) // placeholder moderation ID
            .RuleFor(d => d.DeactivatedByUserId, (f, d) => d.UserModerationId is null ? d.DeactivatedUserId : f.PickRandom(moderatorIds.Where(id => id != d.DeactivatedUserId)))
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
