using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class AccountSignupTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- V1 Signup ---

    [Test]
    public async Task V1Signup_Success_CreatesUser()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/signup", TestHelper.JsonContent(new
        {
            username = "v1user",
            password = "SecurePassword123#",
            email = "v1user@test.org"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "v1user@test.org");
        await Assert.That(user).IsNotNull();
    }

    [Test, DependsOn(nameof(V1Signup_Success_CreatesUser))]
    public async Task V1Signup_DuplicateEmail_Returns409()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/signup", TestHelper.JsonContent(new
        {
            username = "v1userDifferent",
            password = "SecurePassword123#",
            email = "v1user@test.org" // same email
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test, DependsOn(nameof(V1Signup_Success_CreatesUser))]
    public async Task V1Signup_DuplicateUsername_Returns409()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/signup", TestHelper.JsonContent(new
        {
            username = "v1user", // same username
            password = "SecurePassword123#",
            email = "v1different@test.org"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // --- V2 Signup ---

    [Test]
    public async Task V2Signup_Success_CreatesUser()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "v2user",
            password = "SecurePassword123#",
            email = "v2user@test.org",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "v2user@test.org");
        await Assert.That(user).IsNotNull();
    }

    [Test]
    public async Task V2Signup_InvalidTurnstile_Returns403()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "v2blocked",
            password = "SecurePassword123#",
            email = "v2blocked@test.org",
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test, DependsOn(nameof(V2Signup_Success_CreatesUser))]
    public async Task V2Signup_DuplicateEmail_Returns409()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "v2userDifferent",
            password = "SecurePassword123#",
            email = "v2user@test.org", // same email
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // --- Validation ---

    [Test]
    public async Task V2Signup_EmptyUsername_Returns400()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "",
            password = "SecurePassword123#",
            email = "emptyusername@test.org",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task V2Signup_EmptyPassword_Returns400()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "validname",
            password = "",
            email = "emptypass@test.org",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
