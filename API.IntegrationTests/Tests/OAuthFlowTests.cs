using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Web;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.API.OAuth;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// The OAuth endpoints around a not yet activated account: who may activate it, and what the identity that
/// signed up can and cannot do with it.
/// </summary>
/// <remarks>
/// There is no provider to talk to in tests. What the provider round trip leaves behind is the flow cookie,
/// a ticket holding the provider's claims and the properties the flow was started with, so the tests mint
/// that cookie the way the callback would and drive the real endpoints from there. That the authorize
/// endpoint puts the activation token into those properties is covered separately, by reading the state it
/// sends to the provider.
/// </remarks>
public sealed class OAuthFlowTests
{
    private const string Provider = "discord";

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- authorize ---

    [Test]
    public async Task Authorize_WithActivationToken_CarriesItThroughTheProviderRoundTrip()
    {
        using var client = CreateClient();
        var response = await client.PostAsync($"/1/oauth/{Provider}/authorize?flow=LoginOrCreate&activationToken=the-mailed-token", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(OAuthUtil.TryGetActivationToken(ReadState(response), out var carried)).IsTrue();
        await Assert.That(carried).IsEqualTo("the-mailed-token");
    }

    [Test]
    public async Task Authorize_LinkFlow_DoesNotCarryAnActivationToken()
    {
        // Linking is for an account that is already signed in, so it never activates anything.
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("oauthlinkauth"), TestHelper.UniqueEmail("oauth-link-auth"), "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.PostAsync($"/1/oauth/{Provider}/authorize?flow=Link&activationToken=the-mailed-token", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(OAuthUtil.TryGetActivationToken(ReadState(response), out _)).IsFalse();
    }

    // --- handoff: activating a pending account ---

    [Test]
    public async Task Handoff_LinkedIdentityWithTheMailedToken_ActivatesAndSignsIn()
    {
        var externalId = NewExternalId();
        var (userId, token) = await CreatePendingOAuthAccountAsync("oauth-handoff-activate", externalId);

        using var client = CreateClient();
        var response = await HandoffAsync(client, MintFlowCookie(externalId, activationToken: token));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location!.AbsolutePath).IsEqualTo("/home");
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsTrue();
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    [Test]
    public async Task Handoff_LinkedIdentityWithoutTheToken_StaysPending_AndIsNotSignedIn()
    {
        // The identity that signed up, on its own: it never showed it reads the inbox.
        var externalId = NewExternalId();
        var (userId, _) = await CreatePendingOAuthAccountAsync("oauth-handoff-notoken", externalId);

        using var client = CreateClient();
        var response = await HandoffAsync(client, MintFlowCookie(externalId));

        await AssertSentBackToSignupAsync(response, pending: true);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Handoff_LinkedIdentityWithATokenOfAnotherAccount_StaysPending()
    {
        // A link from an inbox the caller does control does not activate the account they parked elsewhere.
        var externalId = NewExternalId();
        var (userId, _) = await CreatePendingOAuthAccountAsync("oauth-handoff-othertoken", externalId);
        var (otherUserId, otherToken) = await CreatePendingOAuthAccountAsync("oauth-handoff-othertok2", NewExternalId());

        using var client = CreateClient();
        var response = await HandoffAsync(client, MintFlowCookie(externalId, activationToken: otherToken));

        await AssertSentBackToSignupAsync(response, pending: true);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
        await Assert.That(await IsActivatedAsync(otherUserId)).IsFalse();
    }

    [Test]
    public async Task Handoff_AnotherIdentityWithTheMailedToken_DoesNotActivate()
    {
        // The inbox owner clicking the link and signing in with their own provider account: that identity
        // is not the one on the account somebody else created with their address.
        var (userId, token) = await CreatePendingOAuthAccountAsync("oauth-handoff-otherid", NewExternalId());

        using var client = CreateClient();
        var response = await HandoffAsync(client, MintFlowCookie(NewExternalId(), activationToken: token));

        await AssertSentBackToSignupAsync(response, pending: false);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    // --- signup-finalize ---

    [Test]
    public async Task SignupFinalize_EmailTheProviderDidNotVerify_CreatesAPendingAccount_WithoutASession()
    {
        var externalId = NewExternalId();
        var email = TestHelper.UniqueEmail("oauth-finalize-unverified");

        using var client = CreateClient();
        var response = await FinalizeAsync(client, MintFlowCookie(externalId, email: TestHelper.UniqueEmail("oauth-finalize-provider"), emailVerified: true), email, TestHelper.UniqueUsername("oauthfinunver"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsFalse();
        await Assert.That(SetsCookie(response, AuthConstants.SignupBrowserSecureCookieName)).IsTrue();

        var user = await GetUserByEmailAsync(email);
        await Assert.That(user.ActivatedAt).IsNull();
    }

    [Test]
    public async Task SignupFinalize_EmailTheProviderVerified_CreatesAnActivatedAccount_AndSignsIn()
    {
        var externalId = NewExternalId();
        var email = TestHelper.UniqueEmail("oauth-finalize-verified");

        using var client = CreateClient();
        var response = await FinalizeAsync(client, MintFlowCookie(externalId, email: email, emailVerified: true), email, TestHelper.UniqueUsername("oauthfinver"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsTrue();

        var user = await GetUserByEmailAsync(email);
        await Assert.That(user.ActivatedAt).IsNotNull();
    }

    [Test]
    public async Task SignupFinalize_PendingAccount_NewEmail_MovesIt_AndTheOldLinkNoLongerActivates()
    {
        // Sign up with an inbox of your own, then move the pending account onto someone else's address.
        var externalId = NewExternalId();
        var (userId, oldToken) = await CreatePendingOAuthAccountAsync("oauth-finalize-move", externalId);
        var username = await GetUsernameAsync(userId);
        var victimEmail = TestHelper.UniqueEmail("oauth-finalize-victim");

        using var client = CreateClient();
        var response = await FinalizeAsync(client, MintFlowCookie(externalId), victimEmail, username);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsFalse();
        var moved = await GetUserByEmailAsync(victimEmail);
        await Assert.That(moved.Id).IsEqualTo(userId);
        await Assert.That(moved.ActivatedAt).IsNull();

        // Neither from the signup browser...
        var activate = new HttpRequestMessage(HttpMethod.Post, $"/1/account/activate?token={oldToken}");
        activate.Headers.Add("Cookie", MintSignupBrowserCookie(userId));
        await Assert.That((await client.SendAsync(activate)).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // ...nor through a provider sign-in.
        var handoff = await HandoffAsync(client, MintFlowCookie(externalId, activationToken: oldToken));
        await AssertSentBackToSignupAsync(handoff, pending: true);

        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task SignupFinalize_PendingAccount_EmailTheProviderVerified_ReplacesItWithAnActivatedAccount()
    {
        var externalId = NewExternalId();
        var (pendingUserId, _) = await CreatePendingOAuthAccountAsync("oauth-finalize-replace", externalId);
        var email = TestHelper.UniqueEmail("oauth-finalize-replace-verified");

        using var client = CreateClient();
        var response = await FinalizeAsync(client, MintFlowCookie(externalId, email: email, emailVerified: true), email, TestHelper.UniqueUsername("oauthfinrepl"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsTrue();

        var user = await GetUserByEmailAsync(email);
        await Assert.That(user.Id).IsNotEqualTo(pendingUserId);
        await Assert.That(user.ActivatedAt).IsNotNull();
        await Assert.That(await UserExistsAsync(pendingUserId)).IsFalse();
    }

    [Test]
    public async Task SignupFinalize_PendingAccount_EmailTheProviderVerified_UsernameTaken_KeepsThePendingAccount()
    {
        var externalId = NewExternalId();
        var (pendingUserId, _) = await CreatePendingOAuthAccountAsync("oauth-finalize-keep", externalId);
        var takenUsername = TestHelper.UniqueUsername("oauthfintaken");
        await TestHelper.CreateUserInDb(WebApplicationFactory, takenUsername, TestHelper.UniqueEmail("oauth-finalize-taken"), "TakenPassword123#");
        var email = TestHelper.UniqueEmail("oauth-finalize-keep-verified");

        using var client = CreateClient();
        var response = await FinalizeAsync(client, MintFlowCookie(externalId, email: email, emailVerified: true), email, takenUsername);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(await UserExistsAsync(pendingUserId)).IsTrue();

        // The identity is still linked to it, so the signup can be retried with another username.
        var retry = await FinalizeAsync(client, MintFlowCookie(externalId, email: email, emailVerified: true), email, TestHelper.UniqueUsername("oauthfinretry"));
        await Assert.That(retry.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await UserExistsAsync(pendingUserId)).IsFalse();
    }

    [Test]
    public async Task SignupFinalize_IdentityOfAnActivatedAccount_Returns409()
    {
        var externalId = NewExternalId();
        var (userId, token) = await CreatePendingOAuthAccountAsync("oauth-finalize-activated", externalId);
        using var client = CreateClient();
        await HandoffAsync(client, MintFlowCookie(externalId, activationToken: token));
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();

        var response = await FinalizeAsync(client, MintFlowCookie(externalId), TestHelper.UniqueEmail("oauth-finalize-activated-new"), TestHelper.UniqueUsername("oauthfinact"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // --- the reported takeover, end to end ---

    [Test]
    public async Task AccountSomeoneElseCreatedOnMyAddress_AfterITakeItOverByPasswordReset_TheirIdentityNoLongerSignsIn()
    {
        var attackerExternalId = NewExternalId();
        var victimEmail = TestHelper.UniqueEmail("oauth-takeover-victim");

        // The attacker signs up through the provider, typing the victim's address.
        using var client = CreateClient();
        var signup = await FinalizeAsync(client, MintFlowCookie(attackerExternalId, email: TestHelper.UniqueEmail("oauth-takeover-attacker"), emailVerified: true), victimEmail, TestHelper.UniqueUsername("oauthtakeover"));
        await Assert.That(signup.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var account = await GetUserByEmailAsync(victimEmail);

        // The victim, who owns the inbox, resets the password. That activates the account.
        var reset = await CompleteSeededPasswordResetAsync(account.Id);
        await Assert.That(reset.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(account.Id)).IsTrue();

        // The attacker's identity is no longer linked: signing in with it reaches no account.
        var handoff = await HandoffAsync(client, MintFlowCookie(attackerExternalId));
        await AssertSentBackToSignupAsync(handoff, pending: false);

        // And it cannot be attached to that address again.
        var again = await FinalizeAsync(client, MintFlowCookie(attackerExternalId), victimEmail, TestHelper.UniqueUsername("oauthtakeover2"));
        await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // --- helpers ---

    private HttpClient CreateClient() => WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private static string NewExternalId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// The flow cookie as the provider callback leaves it: the provider's claims, plus the properties the
    /// flow was started with.
    /// </summary>
    private string MintFlowCookie(string externalId, string? email = null, bool emailVerified = false, string? activationToken = null, OAuthFlow flow = OAuthFlow.LoginOrCreate)
    {
        var options = WebApplicationFactory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(OAuthConstants.FlowScheme);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, externalId),
            new(ClaimTypes.Name, "external-" + externalId[..8])
        ];
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));
        if (emailVerified) claims.Add(new Claim(OAuthConstants.ClaimEmailVerified, "true"));

        var properties = new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        properties.Items[".AuthScheme"] = Provider;
        OAuthUtil.SetOAuthFlow(properties, flow);
        OAuthUtil.SetActivationToken(properties, activationToken);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Provider)), properties, OAuthConstants.FlowScheme);

        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }

    private string MintSignupBrowserCookie(Guid userId)
    {
        var provider = WebApplicationFactory.Services.GetRequiredService<IDataProtectionProvider>();
        return $"{AuthConstants.SignupBrowserSecureCookieName}={SignupBrowserCookie.CreateToken(provider, userId)}";
    }

    /// <summary>The properties the authorize endpoint handed to the provider, read back from the state parameter.</summary>
    private AuthenticationProperties ReadState(HttpResponseMessage response)
    {
        var state = HttpUtility.ParseQueryString(response.Headers.Location!.Query)["state"]!;
        var options = WebApplicationFactory.Services.GetRequiredService<IOptionsMonitor<DiscordAuthenticationOptions>>().Get(Provider);
        return options.StateDataFormat.Unprotect(state)!;
    }

    private static Task<HttpResponseMessage> HandoffAsync(HttpClient client, string flowCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/1/oauth/{Provider}/handoff");
        request.Headers.Add("Cookie", flowCookie);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> FinalizeAsync(HttpClient client, string flowCookie, string email, string username)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/1/oauth/{Provider}/signup-finalize")
        {
            Content = TestHelper.JsonContent(new { email, username })
        };
        request.Headers.Add("Cookie", flowCookie);
        return client.SendAsync(request);
    }

    /// <summary>Not signed in, and sent to the signup form: there is no usable account for this identity.</summary>
    private static async Task AssertSentBackToSignupAsync(HttpResponseMessage response, bool pending)
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location!.AbsolutePath).IsEqualTo($"/oauth/{Provider}/create");
        await Assert.That(response.Headers.Location.Query).IsEqualTo(pending ? "?pending=1" : "");
        await Assert.That(SetsCookie(response, AuthConstants.UserSessionCookieName)).IsFalse();
    }

    private static bool SetsCookie(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) &&
        values.Any(c => c.StartsWith(name + "=") && !c.StartsWith(name + "=;"));

    /// <summary>
    /// An account created through the provider with an address it did not verify, with its activation link
    /// already mailed (the Cron delivery job mints the real token at send time).
    /// </summary>
    private async Task<(Guid UserId, string Token)> CreatePendingOAuthAccountAsync(string prefix, string externalId)
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername(prefix.Replace("-", "")), TestHelper.UniqueEmail(prefix), "Placeholder123#", activated: false);
        var token = "activation-" + Guid.NewGuid().ToString("N");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, (string?)null));
        db.UserActivationRequests.Add(new UserActivationRequest { UserId = userId, TokenHash = HashingUtils.HashToken(token) });
        db.UserOAuthConnections.Add(new UserOAuthConnection
        {
            UserId = userId,
            ProviderKey = Provider,
            ExternalId = externalId,
            DisplayName = "external"
        });
        await db.SaveChangesAsync();

        return (userId, token);
    }

    private async Task<HttpResponseMessage> CompleteSeededPasswordResetAsync(Guid userId)
    {
        const string secret = "test-password-reset-secret";
        var resetId = Guid.CreateVersion7();
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var stamp = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.SecurityStamp).SingleAsync();
            db.UserPasswordResets.Add(new UserPasswordReset
            {
                Id = resetId,
                UserId = userId,
                TokenHash = HashingUtils.HashToken(secret),
                SecurityStampAtCreate = stamp
            });
            await db.SaveChangesAsync();
        }

        using var client = WebApplicationFactory.CreateClient();
        return await client.PostAsync($"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = "NewPassword123#" }));
    }

    private async Task<bool> IsActivatedAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ActivatedAt != null).SingleAsync();
    }

    private async Task<bool> UserExistsAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
    }

    private async Task<string> GetUsernameAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();
    }

    private async Task<User> GetUserByEmailAsync(string email)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
    }
}
