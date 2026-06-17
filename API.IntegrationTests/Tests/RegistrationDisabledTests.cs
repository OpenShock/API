using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Verifies that all account-creation endpoints refuse with 403 <c>Signup.RegistrationDisabled</c>
/// when registration is disabled.
/// </summary>
public sealed class RegistrationDisabledTests
{
    [ClassDataSource<RegistrationDisabledWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required RegistrationDisabledWebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task V1Signup_RegistrationDisabled_Returns403WithProblemType()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/signup", TestHelper.JsonContent(new
        {
            username = "disabledv1",
            password = "SecurePassword123#",
            email = "disabledv1@test.org"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Type).IsEqualTo("Signup.RegistrationDisabled");
    }

    [Test]
    public async Task V2Signup_RegistrationDisabled_Returns403WithProblemType()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "disabledv2",
            password = "SecurePassword123#",
            email = "disabledv2@test.org",
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Type).IsEqualTo("Signup.RegistrationDisabled");
    }
}
