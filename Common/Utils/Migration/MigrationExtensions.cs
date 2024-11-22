using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace OpenShock.Common.Utils.Migration;

public static class MigrationExtensions
{
    private static KeyValuePair<string, ComplexMigrationBase<TDbContext>>? GetComplexMigrationRecord<TDbContext>(this Type type) where TDbContext : DbContext
    {
        var complexMigrationType = type
            .GetCustomAttributes(false)
            .Select(x => x.GetType())
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ComplexMigrationAttribute<,>))
            .Select(t => t.GetGenericArguments().First())
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


        var complexMigrationInstance = Activator.CreateInstance(complexMigrationType) as ComplexMigrationBase<TDbContext> ?? throw new Exception("Failed to instantiate ComplexMigrationBase");

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

    private static void MigrationHistory_EnsureCreated(DatabaseFacade db)
    {
        db.ExecuteSqlRaw(
            $"""
            CREATE TABLE IF NOT EXISTS "__ComplexMigrationHistory" (
                "MigrationId" CHARACTER VARYING(150) NOT NULL PRIMARY KEY,
                "Completed" BOOLEAN NOT NULL DEFAULT FALSE,
                "AppliedOn" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """
        );
    }
    private static bool MigrationHistory_Exists(DatabaseFacade db, string migrationId)
    {
        return db.ExecuteSql(
            $"""
            SELECT COUNT(*) FROM "__ComplexMigrationHistory" WHERE "MigrationId" = '{migrationId}'
            """
        ) > 0;
    }
    private static bool MigrationHistory_IsCompleted(DatabaseFacade db, string migrationId)
    {
        return db.ExecuteSql(
            $"""
             SELECT COUNT(*) FROM "__ComplexMigrationHistory" WHERE "MigrationId" = '{migrationId}' AND "Completed" = FALSE
             """
        ) > 0;
    }
    private static List<string> MigrationHistory_ListUncompleted(DatabaseFacade db)
    {
        return db.SqlQueryRaw<string>(
            $"""
            SELECT "MigrationId" FROM "__ComplexMigrationHistory" WHERE "Completed" = FALSE
            """
        ).ToList();
    }
    private static void MigrationHistory_Create(DatabaseFacade db, string migrationId)
    {
        db.ExecuteSql(
            $"""
            INSERT INTO "__ComplexMigrationHistory" ("MigrationId") VALUES ({migrationId});
            """
        );
    }
    private static void MigrationHistory_MarkCompleted(DatabaseFacade db, string migrationId)
    {
        if (db.ExecuteSql(
            $"""
                UPDATE "__ComplexMigrationHistory" SET "Completed" = TRUE WHERE "MigrationId" = '{migrationId}'
            """
            ) != 1)
        {
            throw new Exception($"Failed to mark migration \"{migrationId}\" as completed");
        }
    }

    private static void RunComplexPreMigration<T>(T context, string pendingMigration, bool isMigratingDown, ComplexMigrationBase<T>? migration, ILogger logger) where T : DbContext
    {
        using var transaction = context.Database.BeginTransaction();
        
        if (MigrationHistory_Exists(context.Database, pendingMigration))
        {
            if (migration == null)
            {
                throw new Exception($"Migration \"{pendingMigration}\" exists in database, but not in solution, refusing to migrate to avoid data loss!");
            }
            return;
        }

        if (migration == null)
        {
            return;
        }


        if (isMigratingDown)
            migration.BeforeDown(context, logger);
        else
            migration.BeforeUp(context, logger);
        
        MigrationHistory_Create(context.Database, pendingMigration);
        
        transaction.Commit();
    }
    private static void RunComplexPostMigration<T>(T context, string pendingMigration, bool isMigratingDown, ComplexMigrationBase<T> migration, ILogger logger) where T : DbContext
    {
        using var transaction = context.Database.BeginTransaction();

        if (MigrationHistory_IsCompleted(context.Database, pendingMigration))
        {
            return;
        }
        
        if (isMigratingDown)
            migration.AfterDown(context, logger);
        else
            migration.AfterUp(context, logger);
                    
        MigrationHistory_MarkCompleted(context.Database, pendingMigration);
                    
        transaction.Commit();
    }
    
    private static void RunMigrations<T>(T context, List<string> pendingMigrations, bool isMigratingDown, Dictionary<string, ComplexMigrationBase<T>> complexMigrations, ILogger logger)
        where T : DbContext
    {
        foreach (var pendingMigration in pendingMigrations)
        {
            var complexMigration = complexMigrations.GetValueOrDefault(pendingMigration);
            
            RunComplexPreMigration(context, pendingMigration, isMigratingDown, complexMigration, logger);

            logger.LogInformation("Applying migration [{@Migration}]", pendingMigration);
            context.Database.Migrate(pendingMigration);

            if (complexMigration != null)
            {
                RunComplexPostMigration(context, pendingMigration, isMigratingDown, complexMigration, logger);
            }
        }
    }
    
    public static IApplicationBuilder RunMigrations<T>(this IApplicationBuilder app, ILogger logger) where T : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<T>();
        
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
        var isMigratingDown = appliedMigrations.ToHashSet().Overlaps(pendingMigrations);

        var complexMigrations = context.GetComplexMigrations();

        MigrationHistory_EnsureCreated(context.Database);
        var uncompletedComplexMigrations = MigrationHistory_ListUncompleted(context.Database);
        if (appliedMigrations.Count != 0 && uncompletedComplexMigrations.Any(m => m != appliedMigrations[^1]))
        {
            throw new Exception("Found uncompleted complex migrations that was supposed to run before current database version, refusing to migrate to avoid data loss!");
        }

        var pendingMigrationIds = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrationIds.Count > 0)
        {
            RunMigrations<T>(context, pendingMigrationIds, isMigratingDown, complexMigrations, logger);
        }

        foreach (var uncompletedMigration in uncompletedComplexMigrations)
        {
            if (!complexMigrations.TryGetValue(uncompletedMigration, out var complexMigration)) throw new Exception("Need to run uncompleted migration but could not find it in solution, refusing to migrate to avoid data loss!");
            RunComplexPostMigration(context, uncompletedMigration, isMigratingDown, complexMigration, logger);
        }

        return app;
    }
}
