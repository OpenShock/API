using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;

namespace OpenShock.Common.Utils.Migration;

public static class MigrationExtensions
{
    private static KeyValuePair<string, ComplexMigrationBase<TDbContext>>? GetComplexMigrationRecord<TDbContext>(this Type type) where TDbContext : DbContext
    {
        var complexMigrationType = type
            .GetCustomAttributes(false)
            .Select(x => x.GetType())
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ComplexMigrationAttribute<,>))
            .Select(type => type.GetGenericArguments().First())
            .SingleOrDefault();

        if (complexMigrationType == null)
        {
            return null;
        }

        var migrationId = type.GetCustomAttribute<Microsoft.EntityFrameworkCore.Migrations.MigrationAttribute>(false)?.Id;
        if (string.IsNullOrEmpty(migrationId))
        {
            return null;
        }


        var complexMigrationInstance = Activator.CreateInstance(complexMigrationType) as ComplexMigrationBase<TDbContext> ?? throw new Exception("Failed to instanciate ComplexMigrationBase");

        return new KeyValuePair<string, ComplexMigrationBase<TDbContext>>(migrationId, complexMigrationInstance);
    }

    private static Dictionary<string, ComplexMigrationBase<TDbContext>> GetComplexMigrations<TDbContext>(this TDbContext context) where TDbContext : DbContext
    {
        var efcoreMigrationType = typeof(Microsoft.EntityFrameworkCore.Migrations.Migration);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(type => type.IsClass)
            .Where(efcoreMigrationType.IsAssignableFrom)
            .Select(GetComplexMigrationRecord<TDbContext>)
            .Where(record => record.HasValue)
            .Select(record => record!.Value)
            .ToDictionary();
    }

    public static IApplicationBuilder RunMigrations<T>(this IApplicationBuilder app, ILogger logger) where T : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<T>();

        var pendingMigrationIds = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrationIds.Count <= 0) return app;

        // If pending migrations contains already applied migrations, that must mean that we are reverting migrations
        bool isDown = context.Database.GetAppliedMigrations().ToHashSet().Overlaps(pendingMigrationIds);

        var complexMigrations = context.GetComplexMigrations();

        foreach (var migrationId in pendingMigrationIds)
        {
            var complexMigration = complexMigrations.GetValueOrDefault(migrationId);

            if (complexMigration != null)
            {
                if (isDown)
                {
                    complexMigration.BeforeDown(context, logger);
                }
                else
                {
                    complexMigration.BeforeUp(context, logger);
                }
            }

            logger.LogInformation("Applying migration [{@Migration}]", migrationId);
            context.Database.Migrate(migrationId);

            if (complexMigration != null)
            {
                if (isDown)
                {
                    complexMigration.AfterDown(context, logger);
                }
                else
                {
                    complexMigration.AfterUp(context, logger);
                }
            }
        }

        return app;
    }
}
