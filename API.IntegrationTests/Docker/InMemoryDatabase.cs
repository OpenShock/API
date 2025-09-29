using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

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
            _container ??= new PostgreSqlBuilder()
                .WithNetwork(DockerNetwork.Instance)
                .WithName($"tunit-postgresql-{Guid.CreateVersion7()}")
                .WithImage("postgres:latest")
                .WithPortBinding(5432, 5432)
                .WithDatabase("openshock")
                .WithUsername("openshock")
                .WithPassword("superSecurePassword")
                .Build();

            return _container;
        }
    }
    
    public Task InitializeAsync() => Container.StartAsync();
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}