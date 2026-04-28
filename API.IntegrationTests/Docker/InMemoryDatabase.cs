using OpenShock.Common.Utils;
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
            _container ??= new PostgreSqlBuilder(image: "postgres:latest")
                .WithNetwork(DockerNetwork.Instance)
                .WithName($"tunit-postgresql-{Guid.CreateVersion7()}")
                .WithDatabase("openshock")
                .WithUsername("openshock")
                .WithPassword(CryptoUtils.RandomAlphaNumericString(32))
                .Build();

            return _container;
        }
    }
    
    public Task InitializeAsync() => Container.StartAsync();
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}