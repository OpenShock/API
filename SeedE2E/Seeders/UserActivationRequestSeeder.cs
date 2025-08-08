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
        if (allUserIds.Count == 0)
            return;

        var activationFaker = new Faker<UserActivationRequest>()
            .RuleFor(a => a.TokenHash, f =>
            {
                var token = f.Random.AlphaNumeric(AuthConstants.GeneratedTokenLength);
                return HashingUtils.HashToken(token);
            })
            .RuleFor(a => a.EmailSendAttempts, f => f.Random.Number(0, 3))
            .RuleFor(a => a.CreatedAt, f => f.Date.RecentOffset(20).UtcDateTime);

        var requests = new List<UserActivationRequest>(allUserIds.Count);
        for (int i = 0; i < allUserIds.Count; i++)
        {
            var faked = activationFaker.Generate();
            faked.UserId = allUserIds[i];

            requests.Add(faked);
        }

        db.UserActivationRequests.AddRange(requests);
        await db.SaveChangesAsync();
    }
}
