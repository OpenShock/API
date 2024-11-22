using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace OpenShock.Common.Utils.Migration;

public static class ComplexMigrationExtensions
{
    private static KeyValuePair<string, ComplexMigrationBase<TDbContext>>? GetComplexMigrationRecord<TDbContext>(this Type type) where TDbContext : DbContext
    {
        if (!typeof(Microsoft.EntityFrameworkCore.Migrations.Migration).IsAssignableFrom(type))
        {
            return null;
        }

        var migrationId = type.GetCustomAttribute<Microsoft.EntityFrameworkCore.Migrations.MigrationAttribute>(false)?.Id;
        if (string.IsNullOrEmpty(migrationId))
        {
            return null;
        }

        var complexMigrationType = type
            .GetCustomAttributes(false)
            .Select(x => x.GetType())
            .Where(type => type.GetGenericTypeDefinition() == typeof(ComplexMigrationAttribute<,>))
            .Select(type => type.GetGenericArguments().First())
            .SingleOrDefault();

        if (complexMigrationType == null)
        {
            return null;
        }

        var complexMigrationInstance = Activator.CreateInstance(complexMigrationType) as ComplexMigrationBase<TDbContext> ?? throw new Exception("Failed to instanciate ComplexMigrationBase");

        return new KeyValuePair<string, ComplexMigrationBase<TDbContext>>(migrationId, complexMigrationInstance);
    }

    public static Dictionary<string, ComplexMigrationBase<TDbContext>> GetComplexMigrations<TDbContext>(this TDbContext context) where TDbContext : DbContext
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Select(GetComplexMigrationRecord<TDbContext>)
            .Where(record => record.HasValue)
            .Select(record => record!.Value)
            .ToDictionary();
    }
}
