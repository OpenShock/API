using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

using OpenShock.Internal.Common.Utils;

namespace OpenShock.API.IntegrationTests.Docker;

public sealed class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }
    
    private PostgreSqlContainer? _container;
    public PostgreSqlContainer Container
    {
        get
        {
            _container ??= new PostgreSqlBuilder(image: "postgres:latest")
                .WithNetwork(DockerNetwork.Instance)
                .WithName($"tunit-postgresql-{Guid.CreateVersion7()}")
                .WithDatabase("openshock")
                .WithUsername("openshock")
                .WithPassword(CryptoUtils.RandomString(32))
                .Build();

            return _container;
        }
    }

    /// <summary>
    /// Starts the container and brings the schema up to date. Migrations are applied here, once per
    /// test session, rather than by the API hosts on boot: several <see cref="WebApplicationFactory"/>
    /// variants share this one database, and each boots its own host. EF's migrator is not safe to run
    /// concurrently against the same database, so letting each host migrate on startup races - two
    /// migrators both see the same pending migrations and both apply them, which fails with
    /// "type/relation/collation already exists", a duplicate key on __EFMigrationsHistory, or queries
    /// hitting a half-migrated schema. The factories set OPENSHOCK__DB__SKIPMIGRATION so this is the
    /// only migrator, matching how Cron.IntegrationTests already seeds its schema.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Container.StartAsync();

        await using var migrationContext = new MigrationOpenShockContext(
            Container.GetConnectionString(), false, NullLoggerFactory.Instance);
        await migrationContext.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}