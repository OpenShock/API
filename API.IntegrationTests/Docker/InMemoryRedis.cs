using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace OpenShock.API.IntegrationTests.Docker;

public sealed class InMemoryRedis : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }
    
    private RedisContainer? _container;
    public RedisContainer Container
    {
        get
        {
            _container ??= new RedisBuilder()
                .WithNetwork(DockerNetwork.Instance)
                .WithName($"tunit-redis-{Guid.CreateVersion7()}")
                .WithImage("redis/redis-stack-server:latest")
                .WithPortBinding(6379, 6379)
                .Build();

            return _container;
        }
    }
    
    public Task InitializeAsync() => Container.StartAsync();
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}