using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using TUnit.Core.Interfaces;

namespace OpenShock.API.IntegrationTests.Docker;

public sealed class InMemoryMailpit : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    private IContainer? _container;
    public IContainer Container
    {
        get
        {
            _container ??= new ContainerBuilder("axllent/mailpit:latest")
                .WithNetwork(DockerNetwork.Instance)
                .WithName($"tunit-mailpit-{Guid.CreateVersion7()}")
                .WithPortBinding(1025, true)
                .WithPortBinding(8025, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/info")))
                .Build();

            return _container;
        }
    }

    public string SmtpHost => Container.Hostname;
    public int SmtpPort => Container.GetMappedPublicPort(1025);
    public string ApiBaseUrl => $"http://{Container.Hostname}:{Container.GetMappedPublicPort(8025)}";

    public Task InitializeAsync() => Container.StartAsync();
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
