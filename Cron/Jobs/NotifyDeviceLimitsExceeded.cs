using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Webhook;
using OpenShock.Cron.Attributes;
using System.Collections.Generic;
using System.Drawing;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Notifies via webhook if some users have more devices registered than they should have
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class NotifyDeviceLimitsExceeded
{
    private readonly OpenShockContext _db;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<NotifyDeviceLimitsExceeded> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="webhookService"></param>
    /// <param name="logger"></param>
    public NotifyDeviceLimitsExceeded(OpenShockContext db, IWebhookService webhookService, ILogger<NotifyDeviceLimitsExceeded> logger)
    {
        _db = db;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        // Find users who exceed the hard device limit
        var limit = HardLimits.MaxDevicesPerUser;
        var violators = await _db.Users
            .Select(u => new { u.Id, u.Name, DeviceCount = u.Devices.Count() })
            .Where(x => x.DeviceCount > limit)
            .ToArrayAsync();

        if (violators.Any())
        {
            // Build a markdown list of offending users
            var lines = string.Join('\n', violators.Select(v =>
                $"• **{v.Name}** (`{v.Id}`) — Devices: {v.DeviceCount}"));

            await _webhookService.SendWebhook(
                "HardDeviceLimitExceeded",
                "🚨 Hard Device Limit Exceeded",
                $"""
                The following user accounts exceed the maximum of {limit} devices:

                {lines}
                """,
                Color.OrangeRed
            );
        }

        return violators.Length;
    }
}