using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.Services.Account;

namespace API.IntegrationTests;

public class UserTests : BaseIntegrationTest
{
    [Test]
    [NotInParallel]
    public async Task Create_ShouldAdd_NewUserToDatabase()
    {
        var restult = await AccountService.CreateAccount("test@test.com", "Test123", "MyPassword123");
        if (restult.IsT1) throw new Exception("NO");

        await AccountService.Login("Test123", "MyPassword123", new LoginContext("AAA", "127.0.0.1"));
    }

    [Test]
    [NotInParallel]
    public async Task Create_ShouldAdd_NewUserToDatabase2()
    {
        var restult = await AccountService.CreateAccount("test2@test.com", "Test124", "MyPassword123");
        if (restult.IsT1) throw new Exception("NO");

        await AccountService.Login("Test124", "MyPassword123", new LoginContext("AAA", "127.0.0.1"));
    }
}
