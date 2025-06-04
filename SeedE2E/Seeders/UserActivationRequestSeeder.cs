using Bogus;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserActivationRequestSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.UserActivationRequests.Any())
            return;

        logger.LogInformation("Generating UserActivationRequests...");

        var allUserIds = db.Users.Where(u => u.ActivatedAt == default).Select(u => u.Id).ToList();
        if (!allUserIds.Any())
            return;

        var activationFaker = new Faker<UserActivationRequest>()
            .RuleFor(a => a.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(a => a.SecretHash, f =>
            {
                var raw = f.Random.AlphaNumeric(20);
                return HashingUtils.HashToken(raw).Truncate(HardLimits.UserActivationRequestSecretMaxLength);
            })
            .RuleFor(a => a.EmailSendAttempts, f => f.Random.Number(0, 3))
            .RuleFor(a => a.CreatedAt, f => f.Date.RecentOffset(20).UtcDateTime);

        var requests = activationFaker.Generate(allUserIds.Count);
        db.UserActivationRequests.AddRange(requests);
        await db.SaveChangesAsync();
    }
}
