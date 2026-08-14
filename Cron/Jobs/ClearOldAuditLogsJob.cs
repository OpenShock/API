using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

[CronJob("0 0 * * *")]
public sealed class ClearOldAuditLogsJob
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ClearOldAuditLogsJob> _logger;

    public ClearOldAuditLogsJob(OpenShockContext db, ILogger<ClearOldAuditLogsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cutoff = DateTime.UtcNow - Duration.AuditRetentionTime;
        var deleted = await _db.UserAuditLogs
            .Where(x => x.CreatedAt < cutoff)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Deleted {Count} audit log entries older than {Cutoff}", deleted, cutoff);
        return deleted;
    }
}
