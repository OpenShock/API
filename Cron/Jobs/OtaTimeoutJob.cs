using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

[CronJob("0 */5 * * * ?")]
public sealed class OtaTimeoutJob
{
    private readonly OpenShockContext _db;
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="lifetime">Its stopping token cancels the job when the server shuts down.</param>
    public OtaTimeoutJob(OpenShockContext db, IHostApplicationLifetime lifetime)
    {
        _db = db;
        _lifetime = lifetime;
    }

    public async Task Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var time = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        await _db.DeviceOtaUpdates
            .Where(x => (x.Status == OtaUpdateStatus.Started || x.Status == OtaUpdateStatus.Running) &&
                        x.CreatedAt < time)
            .ExecuteUpdateAsync(calls =>
                calls.SetProperty(x => x.Status, OtaUpdateStatus.Timeout)
                    .SetProperty(x => x.Message, "Timeout reached"), cancellationToken);
    }
}