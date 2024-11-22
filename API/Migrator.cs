using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Utils.Migration;

namespace OpenShock.API;

public static class Migrator
{
    public static IApplicationBuilder RunMigrations<T>(this IApplicationBuilder app, ILogger logger) where T : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<T>();

        var pendingMigrationIds = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrationIds.Count <= 0) return app;

        var complexMigrations = context.GetComplexMigrations();

        foreach (var migrationId in pendingMigrationIds)
        {
            var complexMigration = complexMigrations.GetValueOrDefault(migrationId);

            if (complexMigration != null)
            {
                complexMigration.BeforeUp(context, logger); // TODO: this needs to only run on up
                complexMigration.BeforeDown(context, logger); // TODO: this needs to only run on down
            }

            logger.LogInformation("Applying migration [{@Migration}]", migrationId);
            context.Database.Migrate(migrationId);

            if (complexMigration != null)
            {
                complexMigration.AfterUp(context, logger); // TODO: this needs to only run on up
                complexMigration.AfterDown(context, logger); // TODO: this needs to only run on down
            }
        }

        return app;
    }
}
