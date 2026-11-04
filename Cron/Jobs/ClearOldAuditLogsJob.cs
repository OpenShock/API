using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

[CronJob("0 0 * * *")]
public sealed class ClearOldAuditLogsJob
{
    private readonly OpenShockContext _db;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ClearOldAuditLogsJob> _logger;

    public ClearOldAuditLogsJob(OpenShockContext db, IHostApplicationLifetime lifetime, ILogger<ClearOldAuditLogsJob> logger)
    {
        _db = db;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var cutoff = DateTime.UtcNow - Duration.AuditRetentionTime;
        var deleted = await _db.UserAuditLogs
            .Where(x => x.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} audit log entries older than {Cutoff}", deleted, cutoff);
        return deleted;
    }
}
