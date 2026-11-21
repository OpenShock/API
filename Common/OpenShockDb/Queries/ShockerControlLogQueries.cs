using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

public static class ShockerControlLogQueries
{
    /// <summary>
    /// Skips logs whose id was already written, so retrying a batch can't count it twice, and logs of deleted owners.
    /// </summary>
    public static Task<int> InsertControlLogsAsync(this OpenShockContext db, IReadOnlyCollection<ShockerControlLog> logs,
        CancellationToken cancellationToken = default)
    {
        if (logs.Count == 0) return Task.FromResult(0);

        var ids = logs.Select(x => x.Id).ToArray();
        var ownerIds = logs.Select(x => x.OwnerUserId).ToArray();
        var shockerIds = logs.Select(x => x.ShockerId).ToArray();
        var controllerIds = logs.Select(x => x.ControlledByUserId).ToArray();
        var intensities = logs.Select(x => (short)x.Intensity).ToArray();
        var maxIntensities = logs.Select(x => (short)x.MaxIntensity).ToArray();
        var durations = logs.Select(x => (long)x.Duration).ToArray();
        var types = logs.Select(x => x.Type).ToArray();
        var customNames = logs.Select(x => x.CustomName).ToArray();
        var live = logs.Select(x => x.LiveControl).ToArray();
        var createdAts = logs.Select(x => x.CreatedAt).ToArray();

        return db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO shocker_control_logs (id, owner_user_id, shocker_id, controlled_by_user_id, intensity, max_intensity, duration, type, custom_name, live_control, created_at)
             SELECT v.*
             FROM unnest({ids}, {ownerIds}, {shockerIds}, {controllerIds}, {intensities}, {maxIntensities}, {durations}, {types}, {customNames}, {live}, {createdAts})
               AS v(id, owner_user_id, shocker_id, controlled_by_user_id, intensity, max_intensity, duration, type, custom_name, live_control, created_at)
             WHERE EXISTS (SELECT 1 FROM users u WHERE u.id = v.owner_user_id)
             ON CONFLICT DO NOTHING
             """, cancellationToken);
    }
}
