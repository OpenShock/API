using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace OpenShock.API.IntegrationTests.Docker;

public sealed class DockerNetwork : IAsyncInitializer, IAsyncDisposable
{
    public INetwork Instance { get; } = new NetworkBuilder()
        .WithName($"tunit-{Guid.CreateVersion7():N}")
        .Build();
    
    public Task InitializeAsync() => Instance.CreateAsync();
    public ValueTask DisposeAsync() => Instance.DisposeAsync();
}