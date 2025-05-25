using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenShock.API.IntegrationTests;

public class AccountTests : BaseIntegrationTest
{
    [Test]
    public async Task CreateAccount_ShouldAdd_NewUserToDatabase()
    {
        using var client = WebAppFactory.CreateClient();

        var requestBody = JsonSerializer.Serialize(new
        {
            username = "Bob",
            password = "SecurePassword123#",
            email = "bob@example.com",
            turnstileresponse = "valid-token"
        });


        var response = await client.PostAsync("/2/account/signup", new StringContent(requestBody, Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
