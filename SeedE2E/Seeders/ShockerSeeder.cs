﻿using Bogus;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using Microsoft.Extensions.Logging;

namespace OpenShock.SeedE2E.Seeders;

public static class ShockerSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        // Only seed if there are no shockers yet
        if (db.Shockers.Any())
            return;

        logger.LogInformation("Generating Shockers...");

        // Grab all device IDs from the database
        var allDeviceIds = await db.Devices.Select(d => d.Id).ToListAsync();

        var shockerFaker = new Faker<Shocker>()
            .RuleFor(s => s.Id, f => Guid.CreateVersion7())
            .RuleFor(s => s.DeviceId, f => f.PickRandom(allDeviceIds))
            .RuleFor(s => s.Name, f => f.Commerce.ProductAdjective().Truncate(HardLimits.ShockerNameMaxLength))
            .RuleFor(s => s.Model, f => f.PickRandom<ShockerModelType>())
            .RuleFor(s => s.RfId, f => f.Random.UShort())
            .RuleFor(s => s.IsPaused, f => f.Random.Bool(0.2f))
            .RuleFor(s => s.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Generate 11 fake shockers per device
        var bogusShockers = shockerFaker.Generate(allDeviceIds.Count * 11);
        db.Shockers.AddRange(bogusShockers);

        await db.SaveChangesAsync();
    }
}
