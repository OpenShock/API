using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Webhook;
using OpenShock.Cron.Attributes;
using System.Collections.Generic;
using System.Drawing;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes shocker control logs by enforcing a maximum log count per user
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class NotifyLimitsExceeded
{
    private readonly OpenShockContext _db;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<NotifyLimitsExceeded> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="webhookService"></param>
    /// <param name="logger"></param>
    public NotifyLimitsExceeded(OpenShockContext db, IWebhookService webhookService, ILogger<NotifyLimitsExceeded> logger)
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