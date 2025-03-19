using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.Services.Account;
using OpenShock.Common.OpenShockDb;

namespace API.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IServiceScope _scope;
    protected readonly OpenShockContext DbContext;
    protected readonly IAccountService  AccountService;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        if (DbContext.Database.GetPendingMigrations().Any())
        {
            DbContext.Database.Migrate();
        }
        
        AccountService = _scope.ServiceProvider.GetRequiredService<IAccountService>();
    }
}