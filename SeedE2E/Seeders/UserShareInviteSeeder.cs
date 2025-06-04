using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class UserShareInviteSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.UserShareInvites.Any())
            return;

        logger.LogInformation("Generating UserShareInvites...");

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();
        // Exclude self-invites
        var inviteFaker = new Faker<UserShareInvite>()
            .RuleFor(i => i.Id, f => Guid.CreateVersion7())
            .RuleFor(i => i.OwnerId, f => f.PickRandom(allUserIds))
            .RuleFor(i => i.RecipientUserId, (f, i) =>
            {
                var recipient = f.PickRandom(allUserIds);
                return recipient == i.OwnerId ? f.PickRandom(allUserIds.Where(x => x != i.OwnerId)) : recipient;
            })
            .RuleFor(i => i.CreatedAt, f => f.Date.RecentOffset(45).UtcDateTime);

        // Generate roughly one invite per 3 users
        var invites = inviteFaker.Generate(allUserIds.Count / 3 + 1);
        db.UserShareInvites.AddRange(invites);
        await db.SaveChangesAsync();
    }
}
