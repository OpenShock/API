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
            // Random alphanumeric hash (truncated to the max length)
            .RuleFor(u => u.PasswordHash, f => HashingUtils.HashPassword(f.Random.AlphaNumeric(32)))
            // Created sometime in the past year
            .RuleFor(u => u.CreatedAt, f => f.Date.PastOffset(25).UtcDateTime)
            // Activated between 1 second and 30 days of creation
            .RuleFor(u => u.ActivatedAt, (f, u) => f.Random.Bool(0.1f) ? f.Date.Between(u.CreatedAt.AddSeconds(1), u.CreatedAt.AddDays(30)) : null);

        var adminUserFaker = baseUserFaker.Clone()
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name, null, "openshock.app"))
            .RuleFor(u => u.Roles, _ => [RoleType.Admin]);

        var systemUserFaker = baseUserFaker.Clone()
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name, null, "openshock.app"))
            .RuleFor(u => u.Roles, _ => [RoleType.System]);

        var genericUserFaker = baseUserFaker.Clone()
            // Email depends on Name
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            // Default “no roles” (we'll override Roles when we clone for Admin/System/Generic-type fakes)
            .RuleFor(u => u.Roles, f => []);

        // Admin account
        var adminUser = adminUserFaker.Generate();
        adminUser.Name = "Admin";
        adminUser.Email = "test.admin@openshock.app";
        adminUser.PasswordHash = HashingUtils.HashPassword("AdminPassword123!");
        db.Users.Add(adminUser);

        // System account
        var systemUser = systemUserFaker.Generate();
        systemUser.Name = "System";
        systemUser.Email = "test.system@openshock.app";
        systemUser.PasswordHash = "SystemPassword123!";
        db.Users.Add(systemUser);

        // Generic user account
        var genericUser = genericUserFaker.Generate();
        genericUser.Name = "User";
        genericUser.Email = "test.user@openshock.app";
        genericUser.PasswordHash = HashingUtils.HashPassword("UserPassword123!");
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
