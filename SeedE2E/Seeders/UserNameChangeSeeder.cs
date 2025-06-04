using Bogus;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserNameChangeSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.UserNameChanges.Any())
            return;

        var allUserIds = db.Users.Select(u => u.Id).ToList();

        var nameChangeFaker = new Faker<UserNameChange>()
            .RuleFor(n => n.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(n => n.OldName, f => f.Internet.UserNameUnicode().Truncate(HardLimits.UsernameMaxLength))
            .RuleFor(n => n.CreatedAt, f => f.Date.RecentOffset(50).UtcDateTime);

        // Each user could have 0–2 name changes
        var changes = new List<UserNameChange>();
        foreach (var userId in allUserIds)
        {
            var count = new Random().Next(0, 3);
            changes.AddRange(nameChangeFaker.Clone().RuleFor(n => n.UserId, _ => userId).Generate(count));
        }

        db.UserNameChanges.AddRange(changes);
        await db.SaveChangesAsync();
    }
}
