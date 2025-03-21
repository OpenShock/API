namespace API.IntegrationTests;

public abstract class BaseIntegrationTest
{
    [ClassDataSource<IntegrationTestWebAppFactory>(Shared = SharedType.PerTestSession)]
    public required IntegrationTestWebAppFactory WebAppFactory { get; init; }
}