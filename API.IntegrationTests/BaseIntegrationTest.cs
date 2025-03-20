using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.Services.Account;

namespace API.IntegrationTests;

public abstract class BaseIntegrationTest
{
    public BaseIntegrationTest()
    {
        Console.WriteLine("Constructor!");
    }

    [ClassDataSource<IntegrationTestWebAppFactory>(Shared = SharedType.PerAssembly)]
    public required IntegrationTestWebAppFactory WebAppFactory { get; init; }
    public IServiceScope ServiceScope { get; set; }

    public IAccountService AccountService { get; set; }

    [Before(Test)]
    public Task Initialize()
    {
        Console.WriteLine("Initialize!");

        ServiceScope = WebAppFactory.Services.CreateScope();

        AccountService = ServiceScope.ServiceProvider.GetRequiredService<IAccountService>();

        return Task.CompletedTask;
    }
}