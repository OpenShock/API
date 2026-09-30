using System.Net;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Smoke tests that the rate limiter middleware is registered and policies are configured.
/// Rate limiting is disabled in the test server (NoLimiter policies) to avoid interference
/// between tests. Detailed rate limiter behavior is covered by unit tests.
/// </summary>
public sealed class RateLimiterTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task AuthEndpoint_WithRateLimiterPolicy_DoesNotReturn500()
    {
        using var client = WebApplicationFactory.CreateClient();

        // The "auth" rate limiter policy is applied to login/signup endpoints.
        // Verify the policy is correctly registered (no 500 from missing policy).
        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "ratelimitertest@test.org",
            password = "SomePassword123#",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GlobalEndpoint_WithRateLimiterMiddleware_DoesNotReturn500()
    {
        using var client = WebApplicationFactory.CreateClient();

        // Verify that the global rate limiter middleware doesn't cause errors.
        var response = await client.GetAsync("/1");

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }
}
