using OpenShock.API.Services.Account;

namespace API.IntegrationTests;

public class UserTests : BaseIntegrationTest
{
    public UserTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_ShouldAdd_NewUserToDatabase()
    {
        var restult = await AccountService.CreateAccount("test@test.com", "Test123", "MyPassword123");
        if (restult.IsT1) throw new Exception("NO");

        await AccountService.Login("Test123", "MyPassword123", new LoginContext("AAA", "127.0.0.1"));
    }
}
