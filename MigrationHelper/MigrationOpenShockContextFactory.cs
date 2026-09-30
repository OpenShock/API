using Microsoft.EntityFrameworkCore.Design;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.MigrationHelper;

/// <summary>
/// Used by `dotnet ef` design-time tooling to construct <see cref="MigrationOpenShockContext"/>
/// without needing a parameterless constructor on the context itself.
/// </summary>
public sealed class MigrationOpenShockContextFactory : IDesignTimeDbContextFactory<MigrationOpenShockContext>
{
    public MigrationOpenShockContext CreateDbContext(string[] args)
    {
        return new MigrationOpenShockContext("Host=localhost;Database=openshock;Username=openshock;Password=openshock", true);
    }
}
