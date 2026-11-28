using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// The signup browser cookie only stands in for the account's credential when it comes back from the
/// country the signup was made in.
/// </summary>
// One at a time: every test moves the single fake location all requests of this host resolve to. The key is
// the one the LCG assignment suites use, because they share this host and read the same fake location.
[NotInParallel("gateway_assignment")]
public sealed class SignupBrowserCookieCountryTests
{
    [ClassDataSource<GeoLocatedWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required GeoLocatedWebApplicationFactory WebApplicationFactory { get; init; }

    [After(Test)]
    public void ResetCountry() => WebApplicationFactory.ClientCountryCode = null;

    [Test]
    public async Task Activate_FromTheCountryOfTheSignup_ActivatesWithTokenAlone()
    {
        WebApplicationFactory.ClientCountryCode = "NO";
        var (userId, token, cookie) = await SignUpAndGetLinkAsync("countrysame");

        var response = await PostActivateAsync(token, cookie);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    [Test]
    public async Task Activate_FromAnotherCountry_AsksForTheCredential()
    {
        WebApplicationFactory.ClientCountryCode = "NO";
        var (userId, token, cookie) = await SignUpAndGetLinkAsync("countryother");

        WebApplicationFactory.ClientCountryCode = "DE";
        var response = await PostActivateAsync(token, cookie);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();

        // The cookie is only ignored, not burned: back home it still works.
        WebApplicationFactory.ClientCountryCode = "NO";
        await Assert.That((await PostActivateAsync(token, cookie)).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Activate_WhenTheCountryCannotBeResolved_StillAcceptsTheCookie()
    {
        WebApplicationFactory.ClientCountryCode = "NO";
        var (userId, token, cookie) = await SignUpAndGetLinkAsync("countryunknown");

        WebApplicationFactory.ClientCountryCode = null;
        var response = await PostActivateAsync(token, cookie);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    [Test]
    public async Task Activate_CookieFromASignupOfUnknownCountry_IsAcceptedAnywhere()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("countrynone"), TestHelper.UniqueEmail("country-none"), "OwnerPassword123#", activated: false);
        var token = await SeedActivationTokenAsync(userId);
        var provider = WebApplicationFactory.Services.GetRequiredService<IDataProtectionProvider>();
        var cookie = $"{AuthConstants.SignupBrowserSecureCookieName}={SignupBrowserCookie.CreateToken(provider, userId)}";

        WebApplicationFactory.ClientCountryCode = "DE";
        var response = await PostActivateAsync(token, cookie);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private HttpClient CreateClient() => WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    /// <summary>Signs up through the API, and stands in for the Cron job that mails the activation link.</summary>
    private async Task<(Guid UserId, string Token, string Cookie)> SignUpAndGetLinkAsync(string prefix)
    {
        var email = TestHelper.UniqueEmail(prefix);

        using var client = CreateClient();
        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername(prefix),
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith(AuthConstants.SignupBrowserSecureCookieName + "="))
            .Split(';')[0];

        Guid userId;
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            userId = await db.Users.AsNoTracking().Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        }

        return (userId, await SeedActivationTokenAsync(userId), cookie);
    }

    private async Task<string> SeedActivationTokenAsync(Guid userId)
    {
        var token = "activation-" + Guid.NewGuid().ToString("N");
        var hash = HashingUtils.HashToken(token);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var updated = await db.UserActivationRequests.Where(r => r.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(r => r.TokenHash, hash));
        if (updated == 0)
        {
            db.UserActivationRequests.Add(new UserActivationRequest { UserId = userId, TokenHash = hash });
            await db.SaveChangesAsync();
        }

        return token;
    }

    private async Task<HttpResponseMessage> PostActivateAsync(string token, string cookie)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/1/account/activate?token={token}");
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private async Task<bool> IsActivatedAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ActivatedAt != null).SingleAsync();
    }
}
