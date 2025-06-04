using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserEmailChangeSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.UserEmailChanges.Any())
            return;

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();

        var emailChangeFaker = new Faker<UserEmailChange>()
            .RuleFor(e => e.Id, f => Guid.CreateVersion7())
            .RuleFor(e => e.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(e => e.Email, f => f.Internet.Email().Truncate(HardLimits.EmailAddressMaxLength))
            .RuleFor(e => e.SecretHash, f =>
            {
                var raw = f.Random.AlphaNumeric(20);
                return HashingUtils.HashPassword(raw).Truncate(HardLimits.UserEmailChangeSecretMaxLength);
            })
            .RuleFor(e => e.UsedAt, f => f.Random.Bool(0.4f) ? f.Date.RecentOffset(20).UtcDateTime : (DateTime?)null)
            .RuleFor(e => e.CreatedAt, f => f.Date.RecentOffset(40).UtcDateTime);

        // One change per 5 users
        var changes = emailChangeFaker.Generate(allUserIds.Count / 5 + 1);
        db.UserEmailChanges.AddRange(changes);
        await db.SaveChangesAsync();
    }
}
