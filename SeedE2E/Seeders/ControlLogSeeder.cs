﻿using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Seeders;

public static class ControlLogSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        // Only seed if there are no control logs yet
        if (db.ShockerControlLogs.Any())
            return;

        logger.LogInformation("Generating ControlLogs...");

        // Grab all shocker IDs and all user IDs
        var allShockerIds = await db.Shockers.Select(s => s.Id).ToListAsync();
        var allUserIds = await db.Users.Select(u => u.Id).ToListAsync();

        var controlLogFaker = new Faker<ShockerControlLog>()
            .RuleFor(l => l.Id, f => Guid.CreateVersion7())
            .RuleFor(l => l.ShockerId, f => f.PickRandom(allShockerIds))
            .RuleFor(l => l.ControlledByUserId, f => f.PickRandom(allUserIds))
            .RuleFor(l => l.Intensity, f => f.Random.Byte(1, 100))
            .RuleFor(l => l.Duration, f => f.Random.UInt(100, 60000))
            .RuleFor(l => l.Type, f => f.PickRandom(ControlType.Sound, ControlType.Vibrate, ControlType.Shock, ControlType.Stop))
            .RuleFor(l => l.LiveControl, f => f.Random.Bool(0.1f))
            .RuleFor(l => l.CreatedAt, f => f.Date.RecentOffset(15).UtcDateTime)
            .RuleFor(l => l.CustomName, f => f.Random.Bool(0.3f) ? f.Commerce.ProductAdjective() : null);

        // Generate roughly 100 logs per Shocker
        var bogusControlLogs = controlLogFaker.Generate(allShockerIds.Count * 100);
        db.ShockerControlLogs.AddRange(bogusControlLogs);

        await db.SaveChangesAsync();
    }
}
