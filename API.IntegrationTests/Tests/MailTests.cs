using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// The API's responsibility for transactional email is to write the correct <see cref="EmailOutboxMessage"/>
/// row (and its business row) atomically - it never sends mail. These tests assert exactly that: the right
/// outbox row is enqueued (type, recipient, coalesce key, payload) or, for rejected requests, that nothing
/// is enqueued. Actual delivery, lazy token minting, the emailed-link flows, and newest-wins coalescing are
/// the Cron host's job and are covered by Cron.IntegrationTests.
/// </summary>
public sealed class MailTests
{
    /// <summary>Just longer than a pending signup keeps its address from an unproven new signup.</summary>
    private static readonly TimeSpan PastPendingSignupHold = Duration.PendingSignupHoldTime + TimeSpan.FromMinutes(1);

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Account Activation ---

    [Test]
    public async Task V2Signup_EnqueuesActivationOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-activation");
        var username = TestHelper.UniqueUsername("mailactivation");
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username,
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Email == email);

        var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
        await Assert.That(row.Type).IsEqualTo(EmailType.AccountActivation);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Pending);
        await Assert.That(row.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.AccountActivation(user.Id));
        await Assert.That(row.Payload[EmailOutboxPayloadKeys.UserId]).IsEqualTo(user.Id.ToString());
    }

    // --- Resend Activation ---

    [Test]
    public async Task V2Signup_Unactivated_SetsActivationResendCookie()
    {
        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, TestHelper.UniqueEmail("mail-signup-cookie"), TestHelper.UniqueUsername("mailsignupcookie"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var setCookie = response.Headers.GetValues("Set-Cookie").ToArray();
        var resendCookie = setCookie.Single(c => c.StartsWith(AuthConstants.ActivationResendCookieName + "="));
        // Readable by the frontend, which shows the resend button only while the cookie exists.
        await Assert.That(resendCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task ResendActivation_UnactivatedUser_EnqueuesActivationOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-resend-activate");
        var username = TestHelper.UniqueUsername("mailresendactivate");

        // Unactivated user with no existing activation request - exercises the create-request path.
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = CreateCookielessClient();
        var response = await PostResendAsync(client, MintResendCookie(userId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // The missing activation request was created, and a fresh activation email was enqueued.
        var hasRequest = await db.UserActivationRequests.AsNoTracking().AnyAsync(r => r.UserId == userId);
        await Assert.That(hasRequest).IsTrue();

        var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
        await Assert.That(row.Type).IsEqualTo(EmailType.AccountActivation);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Pending);
        await Assert.That(row.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.AccountActivation(userId));
    }

    [Test]
    public async Task ResendActivation_WithSignupCookie_EnqueuesAnotherActivationOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-resend-rotate");
        var username = TestHelper.UniqueUsername("mailresendrotate");
        using var client = CreateCookielessClient();

        // Sign up (V2) - creates an unactivated user, its activation request, the first enqueue, and the cookie.
        var signupResponse = await SignUpAsync(client, email, username);
        await Assert.That(signupResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var cookie = signupResponse.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith(AuthConstants.ActivationResendCookieName + "="))
            .Split(';')[0];

        // Move the signup email out of the resend cooldown.
        await BackdateOutboxAsync(email, TimeSpan.FromMinutes(11));

        // Resend enqueues another activation email. The delivery job re-mints the token and supersedes the
        // older row at send time (coalesce key) - that token rotation is covered in Cron.IntegrationTests.
        var resendResponse = await PostResendAsync(client, cookie);
        await Assert.That(resendResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Email == email);

        var rows = await db.EmailOutbox.AsNoTracking().Where(m => m.Recipient == email).ToListAsync();
        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows.All(r => r.Type == EmailType.AccountActivation)).IsTrue();
        await Assert.That(rows.All(r => r.CoalesceKey == EmailOutboxCoalesceKeys.AccountActivation(user.Id))).IsTrue();
    }

    [Test]
    public async Task ResendActivation_WithinCooldown_Returns429_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-cooldown");
        var username = TestHelper.UniqueUsername("mailresendcooldown");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);
        var cookie = MintResendCookie(userId);

        using var client = CreateCookielessClient();
        await Assert.That((await PostResendAsync(client, cookie)).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await PostResendAsync(client, cookie)).StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(1);
    }

    [Test]
    public async Task ResendActivation_DailyCapReached_Returns429()
    {
        var email = TestHelper.UniqueEmail("mail-resend-cap");
        var username = TestHelper.UniqueUsername("mailresendcap");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);
        var cookie = MintResendCookie(userId);

        using var client = CreateCookielessClient();
        for (var i = 0; i < 5; i++)
        {
            await Assert.That((await PostResendAsync(client, cookie)).StatusCode).IsEqualTo(HttpStatusCode.OK);
            // Step past the cooldown each time; only the 24h cap of 5 should stop the sixth.
            await BackdateOutboxAsync(email, TimeSpan.FromMinutes(11));
        }
        await Assert.That((await PostResendAsync(client, cookie)).StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(5);
    }

    [Test]
    public async Task ResendActivation_AlreadyActivatedUser_Returns400_AndRemovesCookie()
    {
        var email = TestHelper.UniqueEmail("mail-resend-activated");
        var username = TestHelper.UniqueUsername("mailresendactivated");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: true);

        using var client = CreateCookielessClient();
        var response = await PostResendAsync(client, MintResendCookie(userId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(DeletesResendCookie(response)).IsTrue();

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(0);
    }

    [Test]
    public async Task ResendActivation_NoCookie_Returns400()
    {
        using var client = CreateCookielessClient();
        var response = await PostResendAsync(client, cookie: null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ResendActivation_TamperedCookie_Returns400_AndRemovesCookie()
    {
        using var client = CreateCookielessClient();
        var response = await PostResendAsync(client, $"{AuthConstants.ActivationResendCookieName}=not-a-valid-token");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(DeletesResendCookie(response)).IsTrue();
    }

    [Test]
    public async Task Login_Success_RemovesActivationResendCookie()
    {
        var email = TestHelper.UniqueEmail("mail-login-clears");
        var username = TestHelper.UniqueUsername("maillogclears");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: true);

        using var client = CreateCookielessClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/2/account/login");
        request.Content = TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        });
        request.Headers.Add("Cookie", MintResendCookie(userId));

        var response = await client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(DeletesResendCookie(response)).IsTrue();
    }

    [Test]
    public async Task Login_UnactivatedUser_CorrectPassword_ReturnsNotActivated_AndEnqueuesActivation()
    {
        var email = TestHelper.UniqueEmail("mail-login-unactivated");
        var username = TestHelper.UniqueUsername("maillogunact");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("Account.AccountNotActivated");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
        await Assert.That(row.Type).IsEqualTo(EmailType.AccountActivation);
        await Assert.That(row.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.AccountActivation(userId));
    }

    [Test]
    public async Task Login_UnactivatedUser_WrongPassword_EnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-login-unact-wrong");
        var username = TestHelper.UniqueUsername("maillogunactw");
        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = "WrongPassword123#",
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(0);
    }

    private HttpClient CreateCookielessClient() => WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private static Task<HttpResponseMessage> SignUpAsync(HttpClient client, string email, string username) =>
        client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username,
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));

    private string MintResendCookie(Guid userId)
    {
        var provider = WebApplicationFactory.Services.GetRequiredService<IDataProtectionProvider>();
        return $"{AuthConstants.ActivationResendCookieName}={ActivationResendCookie.CreateToken(provider, userId)}";
    }

    private string MintSignupBrowserCookie(Guid userId)
    {
        var provider = WebApplicationFactory.Services.GetRequiredService<IDataProtectionProvider>();
        return $"{AuthConstants.SignupBrowserSecureCookieName}={SignupBrowserCookie.CreateToken(provider, userId)}";
    }

    private static Task<HttpResponseMessage> PostResendAsync(HttpClient client, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/1/account/activate/resend");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    // A deleted cookie comes back as an empty value with an expiry in the past.
    private static bool DeletesResendCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) &&
        values.Any(c => c.StartsWith(AuthConstants.ActivationResendCookieName + "=;") &&
                        c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    private async Task<int> CountOutboxAsync(string email)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.EmailOutbox.AsNoTracking().CountAsync(m => m.Recipient == email);
    }

    private async Task BackdateOutboxAsync(string email, TimeSpan by)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.EmailOutbox
            .Where(m => m.Recipient == email)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.CreatedAt, m => m.CreatedAt - by));
    }

    // --- Password Reset ---

    [Test]
    public async Task V1PasswordReset_Retired_Returns410Gone()
    {
        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email = "whatever@test.org" }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task ResetPasswordAlias_Retired_Returns410Gone()
    {
        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/reset-password", TestHelper.JsonContent(new
        {
            email = "whatever@test.org",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task V2PasswordReset_EnqueuesResetOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-v2");
        var username = TestHelper.UniqueUsername("mailpwresetv2");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new
        {
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var reset = await db.UserPasswordResets.AsNoTracking().SingleAsync(r => r.UserId == userId);

        var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
        await Assert.That(row.Type).IsEqualTo(EmailType.PasswordReset);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Pending);
        await Assert.That(row.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.PasswordReset(userId));
        await Assert.That(row.Payload[EmailOutboxPayloadKeys.PasswordResetId]).IsEqualTo(reset.Id.ToString());
    }

    [Test]
    public async Task V2PasswordReset_UnactivatedUser_EnqueuesResetOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-unact");
        var username = TestHelper.UniqueUsername("mailpwresetunact");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new
        {
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
        await Assert.That(row.Type).IsEqualTo(EmailType.PasswordReset);
        await Assert.That(row.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.PasswordReset(userId));
    }

    [Test]
    public async Task PasswordResetComplete_UnactivatedUser_ActivatesAccount()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-activates");
        var username = TestHelper.UniqueUsername("mailpwresetact");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#", activated: false);

        // Stand in for the Cron delivery job: seed the activation request and a reset with a known secret.
        const string secret = "test-password-reset-secret";
        var resetId = Guid.CreateVersion7();
        await using (var seedScope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var user = await seedDb.Users.FirstAsync(u => u.Id == userId);
            seedDb.UserActivationRequests.Add(new UserActivationRequest { UserId = userId, TokenHash = HashingUtils.HashToken("unused-activation-secret") });
            seedDb.UserPasswordResets.Add(new UserPasswordReset
            {
                Id = resetId,
                UserId = userId,
                TokenHash = HashingUtils.HashToken(secret),
                SecurityStampAtCreate = user.SecurityStamp
            });
            await seedDb.SaveChangesAsync();
        }

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync($"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = "NewPassword123#" }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var activatedAt = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ActivatedAt).SingleAsync();
        await Assert.That(activatedAt).IsNotNull();
        await Assert.That(await db.UserActivationRequests.AsNoTracking().AnyAsync(r => r.UserId == userId)).IsFalse();
    }

    // A reset only signs sessions out. On an activated account every OAuth link and API token was put there
    // by its owner, so they stay.
    [Test]
    public async Task PasswordResetComplete_ActivatedAccount_KeepsOAuthConnectionsAndApiTokens()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-recovery");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpwresetrecov"), email, "OldPassword123#");
        await AddOAuthConnectionAsync(userId, "discord");
        await AddOAuthConnectionAsync(userId, "google");
        var (tokenId, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId);

        var response = await CompleteSeededPasswordResetAsync(userId);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserOAuthConnections.AsNoTracking().CountAsync(c => c.UserId == userId)).IsEqualTo(2);
        await Assert.That(await db.ApiTokens.AsNoTracking().AnyAsync(t => t.Id == tokenId)).IsTrue();

        using var tokenClient = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);
        await Assert.That((await tokenClient.GetAsync("/1/users/self")).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // The reset is what activates this account, and the identity it was signed up with may be someone
    // else's (anyone can do an OAuth signup naming this address), so that link must not survive.
    [Test]
    public async Task PasswordResetComplete_UnactivatedOAuthAccount_RemovesConnection()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-oauth-unact");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpwresetoauthu"), email, "OldPassword123#", activated: false);
        await AddOAuthConnectionAsync(userId, "discord");

        var response = await CompleteSeededPasswordResetAsync(userId);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserOAuthConnections.AsNoTracking().CountAsync(c => c.UserId == userId)).IsEqualTo(0);
    }

    // --- A never activated account is a pending signup: it does not keep others off its email ---

    [Test]
    public async Task Signup_EmailHeldByStalePendingAccount_ReplacesIt()
    {
        var email = TestHelper.UniqueEmail("mail-pending-stale");
        var pendingId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpendingstale"), email, "SquatterPassword123#", activated: false);
        await BackdateUserAsync(pendingId, PastPendingSignupHold);

        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailpendingowner"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var holder = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        await Assert.That(holder.Id).IsNotEqualTo(pendingId);
    }

    // An address is only mailed so often, however many signups it takes: each replaced pending account
    // would otherwise start its own count.
    [Test]
    public async Task Signup_AddressAlreadySentTheDailyCapOfActivationEmails_Returns409_AndMailsNothing()
    {
        var email = TestHelper.UniqueEmail("mail-address-cap");

        using var client = CreateCookielessClient();
        for (var i = 0; i < 5; i++)
        {
            var response = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailaddresscap"));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

            // Step the pending account and its mail past the hold, so the next signup replaces it.
            await BackdateOutboxAsync(email, PastPendingSignupHold);
            await BackdatePendingAccountAsync(email, PastPendingSignupHold);
        }

        var sixth = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailaddresscap"));
        await Assert.That(sixth.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(5);
    }

    private async Task BackdatePendingAccountAsync(string email, TimeSpan by)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, u => u.CreatedAt - by));
    }

    // The hold runs from the last activation mail, not only from signup: replacing the account would
    // mail the same inbox again straight after a resend did.
    [Test]
    public async Task Signup_EmailHeldByStalePendingAccount_ThatWasJustMailedAgain_Returns409()
    {
        var email = TestHelper.UniqueEmail("mail-pending-resent");
        var pendingId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpendingresent"), email, "FirstPassword123#", activated: false);
        await BackdateUserAsync(pendingId, PastPendingSignupHold);
        await using (var seedScope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<OpenShockContext>();
            seedDb.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(pendingId, email, "pending"));
            await seedDb.SaveChangesAsync();
        }

        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailpendingthird"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.Users.AsNoTracking().AnyAsync(u => u.Id == pendingId)).IsTrue();
    }

    // While its hold lasts the pending account keeps the address, so its creator has time to open the
    // link and repeated signups cannot mail the inbox more often than a resend could.
    [Test]
    public async Task Signup_EmailHeldByFreshPendingAccount_Returns409()
    {
        var email = TestHelper.UniqueEmail("mail-pending-fresh");
        var pendingId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpendingfresh"), email, "FirstPassword123#", activated: false);

        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailpendingsecond"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.Users.AsNoTracking().AnyAsync(u => u.Id == pendingId)).IsTrue();
    }

    [Test]
    public async Task Signup_EmailHeldByActivatedAccount_Returns409()
    {
        var email = TestHelper.UniqueEmail("mail-pending-activated");
        var ownerId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailpendingact"), email, "OwnerPassword123#");
        await BackdateUserAsync(ownerId, TimeSpan.FromDays(1));

        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, email, TestHelper.UniqueUsername("mailpendingact2"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    private async Task BackdateUserAsync(Guid userId, TimeSpan by)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, u => u.CreatedAt - by));
    }

    // --- Disconnecting a provider must not leave an account with no way to sign in ---

    [Test]
    public async Task RemoveOAuthConnection_OnlyLoginMethod_Returns409_AndKeepsConnection()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailoauthlast"), TestHelper.UniqueEmail("mail-oauth-last"), "Placeholder123#");
        await AddOAuthConnectionAsync(user.Id, "discord");
        await using (var seedScope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await seedDb.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, (string?)null));
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.DeleteAsync("/1/account/connections/discord");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserOAuthConnections.AsNoTracking().CountAsync(c => c.UserId == user.Id)).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveOAuthConnection_PasswordlessAccountWithTwo_RemovesOne_ThenKeepsTheLast()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailoauthtwo"), TestHelper.UniqueEmail("mail-oauth-two"), "Placeholder123#");
        await AddOAuthConnectionAsync(user.Id, "discord");
        await AddOAuthConnectionAsync(user.Id, "google");
        await using (var seedScope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await seedDb.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, (string?)null));
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        await Assert.That((await client.DeleteAsync("/1/account/connections/discord")).StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That((await client.DeleteAsync("/1/account/connections/google")).StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That((await client.DeleteAsync("/1/account/connections/discord")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemoveOAuthConnection_PasswordlessAccountWithTwo_RemovedAtTheSameTime_KeepsOne()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailoauthrace"), TestHelper.UniqueEmail("mail-oauth-race"), "Placeholder123#");
        await AddOAuthConnectionAsync(user.Id, "discord");
        await AddOAuthConnectionAsync(user.Id, "google");
        await using (var seedScope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await seedDb.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, (string?)null));
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var responses = await Task.WhenAll(
            client.DeleteAsync("/1/account/connections/discord"),
            client.DeleteAsync("/1/account/connections/google"));

        await Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.NoContent)).IsEqualTo(1);
        await Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.Conflict)).IsEqualTo(1);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserOAuthConnections.AsNoTracking().CountAsync(c => c.UserId == user.Id)).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveOAuthConnection_AccountWithPassword_RemovesIt()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailoauthpw"), TestHelper.UniqueEmail("mail-oauth-pw"), "OwnerPassword123#");
        await AddOAuthConnectionAsync(user.Id, "discord");

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.DeleteAsync("/1/account/connections/discord");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    // --- Activation needs the emailed token and proof of having signed up ---

    [Test]
    public async Task Activate_FromSignupBrowser_ActivatesWithTokenAlone()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-cookie", "OwnerPassword123#");

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(userId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    // What the owner of the inbox has when someone else signed up with their address.
    [Test]
    public async Task Activate_TokenAloneFromAnotherBrowser_Returns403_AndLeavesAccountUnactivated()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-alone", "OwnerPassword123#");

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("Account.Activation.CredentialRequired");
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();

        // The token stays usable for whoever does know the password.
        var retry = await PostActivateAsync(client, token, password: "OwnerPassword123#");
        await Assert.That(retry.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    [Test]
    public async Task Activate_WrongPassword_Returns403()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-wrongpw", "OwnerPassword123#");

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, password: "NotThePassword123#");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Signup_SetsSignupBrowserCookie_BoundToTheApiHost()
    {
        using var client = CreateCookielessClient();
        var response = await SignUpAsync(client, TestHelper.UniqueEmail("mail-signup-browsercookie"), TestHelper.UniqueUsername("mailsignupbrowser"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // __Host- prefix, no Domain, HttpOnly: a sibling subdomain cannot plant one in someone else's browser.
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(AuthConstants.SignupBrowserSecureCookieName + "="));
        await Assert.That(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(cookie.Contains("path=/", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(cookie.Contains("domain=", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task Activate_ResendCookieAlone_Returns403()
    {
        // The resend cookie is shared across the cookie domain and readable by script, so it proves nothing.
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-resendcookie", "OwnerPassword123#");

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintResendCookie(userId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Activate_SignupBrowserCookieWithoutHostPrefix_Returns403()
    {
        // The unprefixed name is the one a sibling subdomain could set.
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-noprefix", "OwnerPassword123#");
        var value = MintSignupBrowserCookie(userId)[(AuthConstants.SignupBrowserSecureCookieName.Length + 1)..];

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: $"{AuthConstants.SignupBrowserCookieName}={value}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Activate_SignupCookieOfAnotherAccount_Returns403()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-othercookie", "OwnerPassword123#");
        var otherUserId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailactother"), TestHelper.UniqueEmail("mail-activate-other"), "OtherPassword123#", activated: false);

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(otherUserId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Activate_OAuthOnlyAccount_TokenAlone_Returns403_NamingTheProvider()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-activate-oauth", password: null);
        await AddOAuthConnectionAsync(userId, "discord");

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("discord");
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task Activate_UnknownToken_Returns400()
    {
        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, "no-such-activation-token");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RestartPendingSignup_ChangedEmail_InvalidatesTheLinkMailedToTheOldAddress()
    {
        // Sign up with an inbox you control, then move the pending account onto somebody else's address:
        // the link from the first inbox must not activate it there.
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-restart-oldtoken", password: null);
        await AddOAuthConnectionAsync(userId, "discord");
        var victimEmail = TestHelper.UniqueEmail("mail-restart-victim");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();

            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var restarted = await accountService.RestartPendingSignupAsync(userId, victimEmail, username);
            await Assert.That(restarted.Value is User).IsTrue();
        }

        await Assert.That(await GetEmailAsync(userId)).IsEqualTo(victimEmail);

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(userId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task RestartPendingSignup_ChangedEmail_InvalidatesAPasswordResetRequestedForTheOldAddress()
    {
        // The other link an inbox of your own can give you: a reset sets a password and activates.
        var (userId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-oldreset", password: null);
        await AddOAuthConnectionAsync(userId, "discord");
        var (resetId, secret) = await SeedPasswordResetAsync(userId);

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();

            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var restarted = await accountService.RestartPendingSignupAsync(userId, TestHelper.UniqueEmail("mail-restart-victim2"), username);
            await Assert.That(restarted.Value is User).IsTrue();
        }

        using var client = CreateCookielessClient();
        var response = await client.PostAsync($"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = "NewPassword123#" }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await IsActivatedAsync(userId)).IsFalse();
    }

    [Test]
    public async Task RestartPendingSignup_Conflict_LeavesThePendingAccountHoldingTheRequestedAddress()
    {
        var (userId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-conflict", password: null);
        var heldEmail = TestHelper.UniqueEmail("mail-restart-held");
        var holderId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailrestartholder"), heldEmail, "HolderPassword123#", activated: false);
        await BackdateUserAsync(holderId, PastPendingSignupHold);
        var takenUsername = TestHelper.UniqueUsername("mailrestarttaken");
        await TestHelper.CreateUserInDb(WebApplicationFactory, takenUsername, TestHelper.UniqueEmail("mail-restart-taken"), "TakenPassword123#");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var restarted = await accountService.RestartPendingSignupAsync(userId, heldEmail, takenUsername);
        await Assert.That(restarted.Value is AccountWithEmailOrUsernameExists).IsTrue();

        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.Users.AsNoTracking().AnyAsync(u => u.Id == holderId)).IsTrue();
    }

    [Test]
    public async Task RestartPendingSignup_AccountActivatedAfterTheCallerLookedItUp_ChangesNothing()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-restart-stale", "OwnerPassword123#");
        var originalEmail = await GetEmailAsync(userId);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // The request looks the account up while it is pending, as the OAuth connection lookup does...
        var tracked = await db.Users.SingleAsync(u => u.Id == userId);
        await Assert.That(tracked.ActivatedAt).IsNull();

        // ...and it is activated before the restart runs.
        using var client = CreateCookielessClient();
        var activated = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(userId));
        await Assert.That(activated.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var restarted = await accountService.RestartPendingSignupAsync(userId, TestHelper.UniqueEmail("mail-restart-stale-new"), tracked.Name);

        await Assert.That(restarted.Value is User).IsFalse();
        await Assert.That(await GetEmailAsync(userId)).IsEqualTo(originalEmail);
    }

    [Test]
    public async Task RestartPendingSignup_SameEmail_KeepsTheMailedLinkWorking()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-restart-sameemail", password: null);
        await AddOAuthConnectionAsync(userId, "discord");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var current = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.Name, u.Email }).SingleAsync();

            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var restarted = await accountService.RestartPendingSignupAsync(userId, current.Email, current.Name);
            await Assert.That(restarted.Value is User).IsTrue();
        }

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(userId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsActivatedAsync(userId)).IsTrue();
    }

    // Past the account's daily cap no mail could go to the corrected address, and the correction kills the
    // link already mailed: it has to be refused whole, not leave an account with no working link.
    [Test]
    public async Task RestartPendingSignup_ChangedEmail_AccountAtTheDailyCap_ChangesNothing_AndKeepsTheMailedLinkWorking()
    {
        var (userId, token) = await CreateUnactivatedUserWithTokenAsync("mail-restart-cap", password: null);
        await AddOAuthConnectionAsync(userId, "discord");
        var originalEmail = await GetEmailAsync(userId);
        var newEmail = TestHelper.UniqueEmail("mail-restart-cap-new");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();

            // Mailed to a few addresses, as earlier corrections would have: the cap counted is the account's.
            for (var i = 0; i < 5; i++)
            {
                db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(userId, TestHelper.UniqueEmail("mail-restart-cap-earlier"), username));
            }
            await db.SaveChangesAsync();

            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var restarted = await accountService.RestartPendingSignupAsync(userId, newEmail, username);
            await Assert.That(restarted.Value is TooManyActivationResends).IsTrue();
        }

        await Assert.That(await GetEmailAsync(userId)).IsEqualTo(originalEmail);
        await Assert.That(await CountOutboxAsync(newEmail)).IsEqualTo(0);

        using var client = CreateCookielessClient();
        var response = await PostActivateAsync(client, token, cookie: MintSignupBrowserCookie(userId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // The mail to the corrected address is queued by the correction itself, in its transaction: there is
    // no moment where the old link is dead and nothing is on its way to the new address.
    [Test]
    public async Task RestartPendingSignup_ChangedEmail_QueuesTheMailToTheNewAddress()
    {
        var (userId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-queued", password: null);
        var newEmail = TestHelper.UniqueEmail("mail-restart-queued-new");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();

        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var restarted = await accountService.RestartPendingSignupAsync(userId, newEmail, username);

        await Assert.That(restarted.Value is User).IsTrue();
        await Assert.That(await CountOutboxAsync(newEmail)).IsEqualTo(1);
    }

    // The delivery job skips an activation mail whose account has no activation request to mint a token on,
    // so a correction has to leave one behind even where none existed.
    [Test]
    public async Task RestartPendingSignup_ChangedEmail_AccountWithoutActivationRequest_CreatesOne()
    {
        var (userId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-norequest", password: null);
        var newEmail = TestHelper.UniqueEmail("mail-restart-norequest-new");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.UserActivationRequests.Where(r => r.UserId == userId).ExecuteDeleteAsync();
        var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync();

        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var restarted = await accountService.RestartPendingSignupAsync(userId, newEmail, username);

        await Assert.That(restarted.Value is User).IsTrue();
        await Assert.That(await db.UserActivationRequests.AsNoTracking().CountAsync(r => r.UserId == userId)).IsEqualTo(1);
        await Assert.That(await CountOutboxAsync(newEmail)).IsEqualTo(1);
    }

    // The resend an OAuth sign-in triggers on its own stops one short of the account's cap: the last send
    // is kept for correcting the address, which a resend the user asks for may still use.
    [Test]
    public async Task ResendActivationForOAuth_OneSendLeftInTheWindow_LeavesItForACorrection()
    {
        var (userId, _) = await CreateUnactivatedUserWithTokenAsync("mail-oauth-reserve", password: null);
        var email = await GetEmailAsync(userId);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        for (var i = 0; i < 4; i++)
        {
            db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(userId, email, "pending"));
        }
        await db.SaveChangesAsync();
        // Past the cooldown, so only the cap is left to refuse a resend.
        await BackdateOutboxAsync(email, TimeSpan.FromMinutes(11));

        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var automatic = await accountService.ResendActivationEmailForOAuthAsync(userId);
        await Assert.That(automatic.Value is TooManyActivationResends).IsTrue();
        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(4);

        var requested = await accountService.ResendActivationEmailAsync(userId);
        await Assert.That(requested.Value is OpenShock.Common.Results.Success).IsTrue();
        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(5);
    }

    // Each moves onto the address the other holds, so each needs both rows locked: taken in the same order
    // one waits for the other, taken in opposite orders the database would abort one as a deadlock.
    [Test]
    public async Task RestartPendingSignup_TwoSignupsMovingOntoEachOthersAddress_DoNotDeadlock()
    {
        var (firstId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-swap-a", password: null);
        var (secondId, _) = await CreateUnactivatedUserWithTokenAsync("mail-restart-swap-b", password: null);
        await BackdateUserAsync(firstId, PastPendingSignupHold);
        await BackdateUserAsync(secondId, PastPendingSignupHold);
        var firstEmail = await GetEmailAsync(firstId);
        var secondEmail = await GetEmailAsync(secondId);
        await using var usernameScope = WebApplicationFactory.Services.CreateAsyncScope();
        var usernameDb = usernameScope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var usernames = await usernameDb.Users.AsNoTracking()
            .Where(u => u.Id == firstId || u.Id == secondId)
            .ToDictionaryAsync(u => u.Id, u => u.Name);
        var firstUsername = usernames[firstId];
        var secondUsername = usernames[secondId];

        async Task<object> RestartAsync(Guid userId, string email, string username)
        {
            await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            return (await accountService.RestartPendingSignupAsync(userId, email, username)).Value!;
        }

        // Neither call throws. The one that gets there first takes the other's address and removes it.
        var results = await Task.WhenAll(
            RestartAsync(firstId, secondEmail, firstUsername),
            RestartAsync(secondId, firstEmail, secondUsername));

        await Assert.That(results.Count(r => r is User)).IsEqualTo(1);
    }

    // --- An email change is only confirmed from a session of the account that asked for it ---

    [Test]
    public async Task EmailChangeVerify_WithoutSession_Returns401_AndLeavesEmail()
    {
        var (user, token, _) = await CreateUserWithPendingEmailChangeAsync("mail-verify-anon");

        using var client = CreateCookielessClient();
        var response = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(user.Email);
    }

    [Test]
    public async Task EmailChangeVerify_FromAnotherAccountsSession_Returns400_AndLeavesEmail()
    {
        var (user, token, _) = await CreateUserWithPendingEmailChangeAsync("mail-verify-other");
        var other = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailverifysecond"), TestHelper.UniqueEmail("mail-verify-second"), "SecurePassword123#");

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, other.SessionToken);
        var response = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(user.Email);
    }

    [Test]
    public async Task EmailChangeVerify_FromOwnSession_AppliesChange()
    {
        var (user, token, newEmail) = await CreateUserWithPendingEmailChangeAsync("mail-verify-own");

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(newEmail);
    }

    // The route the verification link used before email-change/verify existed still works, under the same rule.
    [Test]
    public async Task EmailChangeVerify_LegacyRoute_FromOwnSession_AppliesChange()
    {
        var (user, token, newEmail) = await CreateUserWithPendingEmailChangeAsync("mail-verify-legacy");

        using var anonymous = CreateCookielessClient();
        var rejected = await anonymous.PostAsync($"/1/account/verify-email?token={token}", null);
        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.PostAsync($"/1/account/verify-email?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(newEmail);
    }

    // --- A never activated account does not keep an existing user from moving to its address ---

    [Test]
    public async Task ChangeEmail_AddressHeldByPendingSignup_IsAccepted()
    {
        const string password = "SecurePassword123#";
        var heldEmail = TestHelper.UniqueEmail("mail-chgemail-pending-held");
        await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailchgemailpending1"), heldEmail, password, activated: false);
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailchgemailpending2"), TestHelper.UniqueEmail("mail-chgemail-pending-own"), password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = heldEmail
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EmailChangeVerify_AddressHeldByPendingSignup_ReleasesIt_AndAppliesChange()
    {
        var (user, token, newEmail) = await CreateUserWithPendingEmailChangeAsync("mail-verify-pending");
        // Fresh, so a plain signup could not replace it yet: opening the mailed link is proof a signup lacks.
        var pendingId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailverifypending"), newEmail, "SquatterPassword123#", activated: false);

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(newEmail);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.Users.AsNoTracking().AnyAsync(u => u.Id == pendingId)).IsFalse();
    }

    [Test]
    public async Task EmailChangeVerify_AddressHeldByActivatedAccount_Returns409_AndLeavesEmail()
    {
        var (user, token, newEmail) = await CreateUserWithPendingEmailChangeAsync("mail-verify-taken");
        await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("mailverifytaken"), newEmail, "HolderPassword123#");

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var response = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(await GetEmailAsync(user.Id)).IsEqualTo(user.Email);
    }

    // Stands in for the Cron delivery job, which mints the real activation token at send time.
    private async Task<(Guid UserId, string Token)> CreateUnactivatedUserWithTokenAsync(string prefix, string? password)
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername(prefix.Replace("-", "")), TestHelper.UniqueEmail(prefix), password ?? "Placeholder123#", activated: false);
        var token = "activation-" + Guid.NewGuid().ToString("N");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        if (password is null)
        {
            await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, (string?)null));
        }
        db.UserActivationRequests.Add(new UserActivationRequest { UserId = userId, TokenHash = HashingUtils.HashToken(token) });
        await db.SaveChangesAsync();

        return (userId, token);
    }

    private static Task<HttpResponseMessage> PostActivateAsync(HttpClient client, string token, string? password = null, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/1/account/activate?token={token}");
        if (password is not null) request.Content = TestHelper.JsonContent(new { password });
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    private async Task<bool> IsActivatedAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ActivatedAt != null).SingleAsync();
    }

    private async Task<(AuthenticatedUser User, string Token, string NewEmail)> CreateUserWithPendingEmailChangeAsync(string prefix)
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername(prefix.Replace("-", "")), TestHelper.UniqueEmail(prefix + "-old"), "SecurePassword123#");
        var newEmail = TestHelper.UniqueEmail(prefix + "-new");
        var token = "email-change-" + Guid.NewGuid().ToString("N");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var stamp = await db.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.SecurityStamp).SingleAsync();
        db.UserEmailChanges.Add(new UserEmailChange
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            OldEmail = user.Email,
            NewEmail = newEmail,
            TokenHash = HashingUtils.HashToken(token),
            SecurityStampAtCreate = stamp
        });
        await db.SaveChangesAsync();

        return (user, token, newEmail);
    }

    private async Task<string> GetEmailAsync(Guid userId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).SingleAsync();
    }

    [Test]
    public async Task PasswordResetComplete_RevokesExistingSessions()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-sessions");
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("mailpwresetsess"), email, "OldPassword123#");

        var response = await CompleteSeededPasswordResetAsync(user.Id);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        var selfResponse = await client.GetAsync("/1/users/self");
        await Assert.That(selfResponse.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // Stands in for the Cron delivery job (which mints the real token), then completes the reset.
    private async Task<(Guid ResetId, string Secret)> SeedPasswordResetAsync(Guid userId)
    {
        const string secret = "test-password-reset-secret";
        var resetId = Guid.CreateVersion7();
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
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

        return (resetId, secret);
    }

    private async Task<HttpResponseMessage> CompleteSeededPasswordResetAsync(Guid userId)
    {
        var (resetId, secret) = await SeedPasswordResetAsync(userId);

        using var client = WebApplicationFactory.CreateClient();
        return await client.PostAsync($"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = "NewPassword123#" }));
    }

    private async Task AddOAuthConnectionAsync(Guid userId, string provider)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        db.UserOAuthConnections.Add(new UserOAuthConnection
        {
            UserId = userId,
            ProviderKey = provider,
            ExternalId = Guid.CreateVersion7().ToString(),
            DisplayName = "external"
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task PasswordResetComplete_LegacyRecoverRoute_Returns410Gone()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync(
            $"/1/account/recover/{Guid.CreateVersion7()}/somesecret",
            TestHelper.JsonContent(new { password = "LegacyNewPassword456#" }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task PasswordResetCheck_LegacyHeadRecoverRoute_Returns410Gone()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Head, $"/1/account/recover/{Guid.CreateVersion7()}/somesecret"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task PasswordResetCheck_InvalidToken_Returns404()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-check-invalid");
        var username = TestHelper.UniqueUsername("mailpwresetcheckinvalid");
        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var bogusId = Guid.CreateVersion7();
        const string bogusSecret = "thisisnotarealtokenatallzz";

        var response = await client.GetAsync($"/1/account/password-reset/{bogusId}/{bogusSecret}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Change Email ---

    [Test]
    public async Task ChangeEmail_EnqueuesVerificationAndNotice()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-notice-old");
        var newEmail = TestHelper.UniqueEmail("mail-chgemail-notice-new");
        var username = TestHelper.UniqueUsername("mailchgemailnotice");
        const string password = "SecurePassword123#";

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var initiateResponse = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = newEmail
        }));
        await Assert.That(initiateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var change = await db.UserEmailChanges.AsNoTracking().SingleAsync(c => c.UserId == user.Id);

        // Verification to the NEW address: coalesced per user, references the change row by id.
        var verification = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == newEmail);
        await Assert.That(verification.Type).IsEqualTo(EmailType.EmailVerification);
        await Assert.That(verification.Status).IsEqualTo(EmailStatus.Pending);
        await Assert.That(verification.CoalesceKey).IsEqualTo(EmailOutboxCoalesceKeys.EmailVerification(user.Id));
        await Assert.That(verification.Payload[EmailOutboxPayloadKeys.EmailChangeId]).IsEqualTo(change.Id.ToString());

        // Notice to the OLD address: always delivered (no coalesce key), carries the new address as data.
        var notice = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == oldEmail);
        await Assert.That(notice.Type).IsEqualTo(EmailType.EmailChangeNotice);
        await Assert.That(notice.CoalesceKey).IsNull();
        await Assert.That(notice.Payload[EmailOutboxPayloadKeys.NewEmail]).IsEqualTo(newEmail);
    }

    [Test]
    public async Task ChangeEmail_WrongPassword_Returns403_AndEnqueuesNothing()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-badpwd-old");
        var newEmail = TestHelper.UniqueEmail("mail-chgemail-badpwd-new");
        var username = TestHelper.UniqueUsername("mailchgemailbadpwd");

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, "CorrectPassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = "WrongPassword!",
            email = newEmail
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var enqueued = await db.EmailOutbox.AsNoTracking()
            .CountAsync(m => m.Recipient == newEmail || m.Recipient == oldEmail);
        await Assert.That(enqueued).IsEqualTo(0);
    }

    [Test]
    public async Task ChangeEmail_Unchanged_Returns400_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-chgemail-unchanged");
        var username = TestHelper.UniqueUsername("mailchgemailunchanged");
        const string password = "SecurePassword123#";

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, email, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email
        }));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var enqueued = await db.EmailOutbox.AsNoTracking().CountAsync(m => m.Recipient == email);
        await Assert.That(enqueued).IsEqualTo(0);
    }

    [Test]
    public async Task ChangeEmail_AlreadyInUse_Returns409()
    {
        var takenEmail = TestHelper.UniqueEmail("mail-chgemail-taken-existing");
        var ownEmail = TestHelper.UniqueEmail("mail-chgemail-taken-own");
        var takenUser = TestHelper.UniqueUsername("mailchgemailtaken1");
        var ownUser = TestHelper.UniqueUsername("mailchgemailtaken2");
        const string password = "SecurePassword123#";

        await TestHelper.CreateUserInDb(WebApplicationFactory, takenUser, takenEmail, password);
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, ownUser, ownEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = takenEmail
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }
}
