using Bogus;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        // If there are already users, assume we’ve seeded before.
        if (db.Users.Any())
            return;

        logger.LogInformation("Generating Users...");

        // --- 1) DEFINE A SINGLE BASE FAKER FOR ALL USERS ---
        //
        // The base faker will set Id, Name, Email, PasswordHash, Roles (defaulted to empty),
        // CreatedAt and ActivatedAt.  We'll override Name/Email/PasswordHash/Roles
        // for each “type” (Admin/System/Generic) below.
        var baseUserFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.CreateVersion7())
            // For random users, generate a username—but we'll override it for “well‐defined” accounts.
            .RuleFor(u => u.Name, f => f.Internet.UserNameUnicode().Truncate(HardLimits.UsernameMaxLength))
            // Email depends on Name
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            // Random alphanumeric hash (truncated to the max length)
            .RuleFor(u => u.PasswordHash, f => HashingUtils.HashPassword(f.Random.AlphaNumeric(32)))
            // Default “no roles” (we'll override Roles when we clone for Admin/System/Generic-type fakes)
            .RuleFor(u => u.Roles, f => [])
            // Created sometime in the past year
            .RuleFor(u => u.CreatedAt, f => f.Date.PastOffset(25).UtcDateTime)
            // Activated between 1 second and 30 days of creation
            .RuleFor(u => u.ActivatedAt, (f, u) => f.Date.Between(u.CreatedAt.AddSeconds(1), u.CreatedAt.AddDays(30)));

        var genericUserFaker = baseUserFaker.Clone();

        var adminUserFaker = baseUserFaker.Clone()
            .RuleFor(u => u.Roles, _ => [RoleType.Admin]);

        var systemUserFaker = baseUserFaker.Clone()
            .RuleFor(u => u.Roles, _ => [RoleType.System]);

        // 2a) Well‐defined Admin account
        var adminUser = adminUserFaker.Clone()
            // Override Name+Email+PasswordHash to our constants
            .RuleFor(u => u.Name, _ => "admin")
            .RuleFor(u => u.Email, _ => "admin@openshock.com")
            .RuleFor(u => u.PasswordHash, _ => HashingUtils.HashPassword("AdminPassword123!"))
            .Generate();

        // 2b) Well‐defined System account
        var systemUser = systemUserFaker.Clone()
            .RuleFor(u => u.Name, _ => "system")
            .RuleFor(u => u.Email, _ => "system@openshock.com")
            .RuleFor(u => u.PasswordHash, _ => HashingUtils.HashPassword("SystemPassword123!"))
            .Generate();

        // 2c) Well‐defined “Generic user” account (no roles)
        var genericUser = baseUserFaker.Clone()
            .RuleFor(u => u.Name, _ => "user")
            .RuleFor(u => u.Email, _ => "user@openshock.com")
            .RuleFor(u => u.PasswordHash, _ => HashingUtils.HashPassword("UserPassword123!"))
            // Roles default to an empty array (clone already set Roles = Array.Empty<RoleType>())
            .Generate();

        // Add those three first
        db.Users.Add(adminUser);
        db.Users.Add(systemUser);
        db.Users.Add(genericUser);

        // --- 3) GENERATE “X” FAKE USERS OF EACH TYPE (Admin / System / Generic) ---

        // You can change this “10” to any other number if you want more/less fakes per role:
        const int numFakePerType = 10;

        // 3a) Fake Admin accounts
        var fakeAdmins = adminUserFaker.Generate(numFakePerType);
        db.Users.AddRange(fakeAdmins);

        // 3b) Fake System accounts
        var fakeSystems = systemUserFaker.Generate(numFakePerType);
        db.Users.AddRange(fakeSystems);

        // 3c) Fake “Generic” user accounts (no roles)
        var fakeGenerics = genericUserFaker.Generate(numFakePerType);
        db.Users.AddRange(fakeGenerics);

        // Save users so that foreign keys can be assigned in subsequent tables
        await db.SaveChangesAsync();
    }
}
