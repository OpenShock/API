using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.SeedE2E.Extensions;

namespace OpenShock.SeedE2E.Seeders;

public static class ApiTokenReportSeeder
{
    public static async Task SeedAsync(OpenShockContext db)
    {
        if (db.ApiTokenReports.Any())
            return;

        var allTokenIds = await db.ApiTokens.Select(t => t.Id).ToListAsync();
        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();

        var reportFaker = new Faker<ApiTokenReport>()
            .RuleFor(r => r.Id, f => Guid.CreateVersion7())
            .RuleFor(r => r.UserId, f => f.PickRandom(allUserIds))
            .RuleFor(r => r.SubmittedCount, f => f.Random.Number(1, 10))
            .RuleFor(r => r.AffectedCount, (f, r) => Math.Min(r.SubmittedCount, f.Random.Number(0, r.SubmittedCount)))
            .RuleFor(r => r.IpAddress, f => f.Internet.IpVAnyAddress(0.4f))
            .RuleFor(r => r.IpCountry, f => f.Address.CountryCode())
            .RuleFor(r => r.CreatedAt, f => f.Date.RecentOffset(14).UtcDateTime);

        // Generate roughly one report per 5 tokens
        var reports = reportFaker.Generate(allTokenIds.Count / 5 + 1);
        db.ApiTokenReports.AddRange(reports);
        await db.SaveChangesAsync();
    }
}
