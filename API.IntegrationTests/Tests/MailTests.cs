using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
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
    public async Task ResendActivation_UnactivatedUser_EnqueuesActivationOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-resend-activate");
        var username = TestHelper.UniqueUsername("mailresendactivate");

        // Unactivated user with no existing activation request — exercises the create-request path.
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));
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
    public async Task ResendActivation_WithExistingRequest_EnqueuesAnotherActivationOutbox()
    {
        var email = TestHelper.UniqueEmail("mail-resend-rotate");
        var username = TestHelper.UniqueUsername("mailresendrotate");
        using var client = WebApplicationFactory.CreateClient();

        // Sign up (V2) — creates an unactivated user, its activation request, and the first enqueue.
        var signupResponse = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username,
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(signupResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Move the signup email out of the resend cooldown.
        await BackdateOutboxAsync(email, TimeSpan.FromMinutes(11));

        // Resend enqueues another activation email. The delivery job re-mints the token and supersedes the
        // older row at send time (coalesce key) - that token rotation is covered in Cron.IntegrationTests.
        var resendResponse = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));
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
    public async Task ResendActivation_WithinCooldown_Returns200_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-cooldown");
        var username = TestHelper.UniqueUsername("mailresendcooldown");
        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(1);
    }

    [Test]
    public async Task ResendActivation_DailyCapReached_EnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-cap");
        var username = TestHelper.UniqueUsername("mailresendcap");
        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
            // Step past the cooldown each time; only the 24h cap of 5 should stop the sixth.
            await BackdateOutboxAsync(email, TimeSpan.FromMinutes(11));
        }

        await Assert.That(await CountOutboxAsync(email)).IsEqualTo(5);
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

    [Test]
    public async Task ResendActivation_AlreadyActivatedUser_Returns200_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-activated");
        var username = TestHelper.UniqueUsername("mailresendactivated");

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: true);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));

        // Generic 200 (no account-state leak), but nothing is enqueued for an already-activated account.
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var enqueued = await db.EmailOutbox.AsNoTracking().CountAsync(m => m.Recipient == email);
        await Assert.That(enqueued).IsEqualTo(0);
    }

    [Test]
    public async Task ResendActivation_UnknownEmail_Returns200_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-unknown");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var enqueued = await db.EmailOutbox.AsNoTracking().CountAsync(m => m.Recipient == email);
        await Assert.That(enqueued).IsEqualTo(0);
    }

    [Test]
    public async Task ResendActivation_InvalidTurnstile_Returns403_AndEnqueuesNothing()
    {
        var email = TestHelper.UniqueEmail("mail-resend-turnstile");
        var username = TestHelper.UniqueUsername("mailresendturnstile");

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/activate/resend", TestHelper.JsonContent(new { email, turnstileResponse = "invalid-token" }));

        // A failed turnstile must stop the send outright - this endpoint mails a third party.
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var enqueued = await db.EmailOutbox.AsNoTracking().CountAsync(m => m.Recipient == email);
        await Assert.That(enqueued).IsEqualTo(0);
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
