using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Webhook;
using OpenShock.Cron.Attributes;
using System.Drawing;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Notifies via webhook if some users have more shockers registered than they should have
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class NotifyShockerLimitsExceeded
{
    private readonly OpenShockContext _db;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<NotifyShockerLimitsExceeded> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="webhookService"></param>
    /// <param name="logger"></param>
    public NotifyShockerLimitsExceeded(OpenShockContext db, IWebhookService webhookService, ILogger<NotifyShockerLimitsExceeded> logger)
    {
        _db = db;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        // Find users who exceed the hard device limit
        var limit = HardLimits.MaxShockersPerDevice;
        var violators = await _db.Users
            .Select(u => new { u.Id, u.Name, MaxShockerCount = u.Devices.Max(d => d.Shockers.Count()) })
            .Where(x => x.MaxShockerCount > limit)
            .ToArrayAsync();

        if (violators.Any())
        {
            // Build a markdown list of offending users
            var lines = string.Join('\n', violators.Select(v =>
                $"• **{v.Name}** (`{v.Id}`) — Max Shockers per Device: {v.MaxShockerCount}"));

            await _webhookService.SendWebhook(
                "HardShockerLimitExceeded",
                "🚨 Hard Shocker Limit Exceeded",
                $"""
                The following user accounts exceed the maximum of {limit} shockers:

                {lines}
                """,
                Color.OrangeRed
            );
        }

        return violators.Length;
    }
}