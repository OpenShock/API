using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

public class AccountTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
    
    [Test]
    public async Task CreateAccount_ShouldAdd_NewUserToDatabase()
    {
        using var client = WebApplicationFactory.CreateClient();

        var requestBody = new
        {
            username = "Bob",
            password = "SecurePassword123#",
            email = "bob@example.com",
            turnstileresponse = "valid-token"
        };


        var response = await client.PostAsJsonAsync("/2/account/signup", requestBody);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "bob@example.com");

        await Assert.That(user).IsNotNull();
    }

    [Test, DependsOn(nameof(CreateAccount_ShouldAdd_NewUserToDatabase))]
    public async Task CheckUsername()
    {
        using var client = WebApplicationFactory.CreateClient();
        
        var requestBody = new { username = "Bob" };
        
        var response = await client.PostAsJsonAsync("/1/account/username/check", requestBody);
        
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        
        // Validate Availability
        var availability = root.GetProperty("availability").GetString();
        await Assert.That(availability).IsEqualTo("Taken");
        
        // Validate Error
        var error = root.GetProperty("error").GetString();
        await Assert.That(error).IsNull();
    }
}
