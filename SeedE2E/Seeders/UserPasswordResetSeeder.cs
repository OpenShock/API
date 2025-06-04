using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserPasswordResetSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.UserPasswordResets.Any())
            return;

        logger.LogInformation("Generating UserPasswordResets...");

        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();

        var prFaker = new Faker<UserPasswordReset>()
            .RuleFor(p => p.Id, f => Guid.CreateVersion7())
            .RuleFor(p => p.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(p => p.SecretHash, f =>
            {
                var raw = f.Random.AlphaNumeric(20);
                return HashingUtils.HashPassword(raw).Truncate(HardLimits.PasswordResetSecretMaxLength);
            })
            .RuleFor(p => p.UsedAt, f => f.Random.Bool(0.3f) ? f.Date.RecentOffset(15).UtcDateTime : null)
            .RuleFor(p => p.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Roughly 2 resets per user
        var resets = prFaker.Generate(allUserIds.Count * 2);
        db.UserPasswordResets.AddRange(resets);
        await db.SaveChangesAsync();
    }
}
