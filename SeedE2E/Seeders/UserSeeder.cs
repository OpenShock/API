using Bogus;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        // If there are already users, assume we’ve seeded before.
        if (db.Users.Any())
            return;

        // Create two well‐defined accounts: Admin + System user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "admin",
            Email = "admin@openshock.com",
            PasswordHash = HashingUtils.HashPassword("AdminPassword123!"),
            Roles = [RoleType.Admin],
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };

        var systemUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "system",
            Email = "system@openshock.com",
            PasswordHash = HashingUtils.HashPassword("SystemPassword123!"),
            Roles = [RoleType.System],
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };

        db.Users.Add(adminUser);
        db.Users.Add(systemUser);

        // “Support” user faker
        var supportUserFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Internet.UserName().Truncate(HardLimits.UsernameMaxLength))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            .RuleFor(u => u.PasswordHash, f => HashingUtils.HashPassword(f.Random.AlphaNumeric(HardLimits.PasswordMaxLength)))
            .RuleFor(u => u.Roles, f => [RoleType.Support])
            .RuleFor(u => u.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
            .RuleFor(u => u.ActivatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Unprivledged user faker
        var unprivledgedUserFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Internet.UserName().Truncate(HardLimits.UsernameMaxLength))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            .RuleFor(u => u.PasswordHash, f => HashingUtils.HashPassword(f.Random.AlphaNumeric(HardLimits.PasswordMaxLength)))
            .RuleFor(u => u.Roles, f => [])
            .RuleFor(u => u.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
            .RuleFor(u => u.ActivatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Generate 10 bogus “Support” users with Bogus
        var bogusSupportUsers = supportUserFaker.Generate(10);
        db.Users.AddRange(bogusSupportUsers);

        // Generate 100 bogus unprivledged users with Bogus
        var bogusUnprivledgedUsers = unprivledgedUserFaker.Generate(100);
        db.Users.AddRange(bogusUnprivledgedUsers);

        // Save users so that foreign keys can be assigned in subsequent tables
        await db.SaveChangesAsync();
    }
}
