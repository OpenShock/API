﻿using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Cron.Jobs;

public sealed class OtaTimeoutJob
{
    private readonly OpenShockContext _db;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    public OtaTimeoutJob(OpenShockContext db)
    {
        _db = db;
    }

    public async Task Execute()
    {
        var time = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        await _db.DeviceOtaUpdates
            .Where(x => (x.Status == OtaUpdateStatus.Started || x.Status == OtaUpdateStatus.Running) &&
                        x.CreatedOn < time)
            .ExecuteUpdateAsync(calls =>
                calls.SetProperty(x => x.Status, OtaUpdateStatus.Timeout)
                    .SetProperty(x => x.Message, "Timeout reached"));
    }
}