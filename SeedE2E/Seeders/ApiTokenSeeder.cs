using Bogus;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.SeedE2E.Extensions;

namespace OpenShock.SeedE2E.Seeders;

public static class ApiTokenSeeder
{
    private static readonly PermissionType[] PermissionTypes = Enum.GetValues<PermissionType>().Cast<PermissionType>().ToArray();

    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.ApiTokens.Any())
            return;

        var allUserIds = db.Users.Select(u => u.Id).ToList();

        var apiTokenFaker = new Faker<ApiToken>()
            .RuleFor(t => t.Id, f => Guid.CreateVersion7())
            .RuleFor(t => t.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(t => t.Name, f => f.Lorem.Word().Truncate(HardLimits.ApiKeyNameMaxLength))
            .RuleFor(t => t.TokenHash, f =>
            {
                // Simulate generating a raw key and hashing it
                var raw = f.Random.AlphaNumeric(40);
                return HashingUtils.HashToken(raw).Truncate(HardLimits.Sha256HashHexLength);
            })
            .RuleFor(t => t.CreatedByIp, f => f.Internet.IpVAnyAddress(0.4f))
            .RuleFor(t => t.Permissions, f =>
            {
                // Random subset of permissions (possibly empty)
                var take = f.Random.Number(0, PermissionTypes.Length);
                return f.PickRandom(PermissionTypes, take).ToList();
            })
            .RuleFor(t => t.ValidUntil, f => f.Date.FutureOffset(1).UtcDateTime)
            .RuleFor(t => t.ValidUntil, f => f.Date.PastOffset(1).UtcDateTime)
            .RuleFor(t => t.LastUsed, _ => DateTime.UnixEpoch);

        // Generate roughly 3 tokens per user
        var tokens = apiTokenFaker.Generate(allUserIds.Count * 3);
        db.ApiTokens.AddRange(tokens);
        await db.SaveChangesAsync();
    }
}
