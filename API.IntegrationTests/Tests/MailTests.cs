using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Tests that verify emails are actually delivered via SMTP to Mailpit.
/// Each test uses a unique recipient address via <see cref="TestHelper.UniqueEmail"/> so Mailpit
/// lookups never collide with other tests in the session.
/// </summary>
public sealed partial class MailTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Account Activation ---

    [Test]
    public async Task V2Signup_SendsAccountActivationEmail()
    {
        var email = TestHelper.UniqueEmail("mail-activation");
        var username = TestHelper.UniqueUsername("mailactivation");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username,
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        await Assert.That(message!.To?.Select(c => c.Address)).Contains(email);
    }

    [Test]
    public async Task ActivationFlow_ViaEmailLink_ActivatesAccount()
    {
        var email = TestHelper.UniqueEmail("mail-activate-flow");
        var username = TestHelper.UniqueUsername("mailactivateflow");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();
        using var client = WebApplicationFactory.CreateClient();

        // Sign up — this triggers an activation email
        var signupResponse = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username,
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(signupResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Wait for and retrieve the activation email
        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();

        var fullMessage = await mailpit.GetMessageAsync(message!.Id);
        await Assert.That(fullMessage).IsNotNull();

        // Extract the activation token from the link in the email HTML
        var token = ExtractQueryParam(fullMessage!.Html, "token");
        await Assert.That(token).IsNotNull().And.IsNotEmpty();

        // Use the token to activate the account
        var activateResponse = await client.PostAsync($"/1/account/activate?token={token}", null);
        await Assert.That(activateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Confirm the user is now activated in the DB
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.ActivatedAt).IsNotNull();
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
    public async Task V2PasswordReset_SendsPasswordResetEmail()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-v2");
        var username = TestHelper.UniqueUsername("mailpwresetv2");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new
        {
            email,
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        await Assert.That(message!.To?.Select(c => c.Address)).Contains(email);
    }

    [Test]
    public async Task PasswordResetFlow_ViaEmailLink_ChangesPassword()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-flow");
        var username = TestHelper.UniqueUsername("mailpwresetflow");
        const string newPassword = "NewSecurePassword456#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        // Initiate password reset
        var resetResponse = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new { email, turnstileResponse = "valid-token" }));
        await Assert.That(resetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Wait for reset email and extract the link
        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();

        var fullMessage = await mailpit.GetMessageAsync(message!.Id);
        await Assert.That(fullMessage).IsNotNull();

        // Link format: /#/account/password/recover/{id}/{secret}
        var (resetId, secret) = ExtractPasswordResetParams(fullMessage!.Html);
        await Assert.That(resetId).IsNotNull().And.IsNotEmpty();
        await Assert.That(secret).IsNotNull().And.IsNotEmpty();

        // Verify the reset token is valid
        var checkResponse = await client.GetAsync($"/1/account/password-reset/{resetId}/{secret}");
        await Assert.That(checkResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Complete the reset with a new password
        var completeResponse = await client.PostAsync(
            $"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = newPassword }));
        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Confirm we can log in with the new password
        var loginResponse = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = newPassword,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Change Email ---

    [Test]
    public async Task ChangeEmailFlow_ViaEmailLink_ChangesEmail()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-flow-old");
        var newEmail = TestHelper.UniqueEmail("mail-chgemail-flow-new");
        var username = TestHelper.UniqueUsername("mailchgemailflow");
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Initiate the email change
        var initiateResponse = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = newEmail
        }));
        await Assert.That(initiateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // The verification email goes to the NEW address
        var message = await mailpit.WaitForMessageAsync(newEmail);
        await Assert.That(message).IsNotNull();

        var fullMessage = await mailpit.GetMessageAsync(message!.Id);
        await Assert.That(fullMessage).IsNotNull();

        var token = ExtractQueryParam(fullMessage!.Html, "token");
        await Assert.That(token).IsNotNull().And.IsNotEmpty();

        // Email is not changed yet
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var beforeUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            await Assert.That(beforeUser.Email).IsEqualTo(oldEmail);
        }

        // Use the token to complete the change
        using var anonClient = WebApplicationFactory.CreateClient();
        var verifyResponse = await anonClient.PostAsync($"/1/account/email-change/verify?token={token}", null);
        await Assert.That(verifyResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Email is now updated
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var afterUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            await Assert.That(afterUser.Email).IsEqualTo(newEmail);
        }

        // Re-using the same token must now fail
        var replayResponse = await anonClient.PostAsync($"/1/account/email-change/verify?token={token}", null);
        await Assert.That(replayResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ChangeEmail_WrongPassword_Returns403_AndSendsNoEmail()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-badpwd-old");
        var newEmail = TestHelper.UniqueEmail("mail-chgemail-badpwd-new");
        var username = TestHelper.UniqueUsername("mailchgemailbadpwd");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, "CorrectPassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = "WrongPassword!",
            email = newEmail
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        // Neither the verification (new address) nor the change notice (old address) should have been dispatched.
        var verification = await mailpit.WaitForMessageAsync(newEmail, TimeSpan.FromSeconds(2));
        await Assert.That(verification).IsNull();
        var notice = await mailpit.WaitForMessageAsync(oldEmail, TimeSpan.FromSeconds(2));
        await Assert.That(notice).IsNull();
    }

    [Test]
    public async Task ChangeEmailFlow_SendsNoticeToOldEmail()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-notice-old");
        var newEmail = TestHelper.UniqueEmail("mail-chgemail-notice-new");
        var username = TestHelper.UniqueUsername("mailchgemailnotice");
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var initiateResponse = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = newEmail
        }));
        await Assert.That(initiateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verification email lands at the new address...
        var verification = await mailpit.WaitForMessageAsync(newEmail);
        await Assert.That(verification).IsNotNull();
        await Assert.That(verification!.To?.Select(c => c.Address)).Contains(newEmail);

        // ...and a notice lands at the OLD address, mentioning the new address.
        var notice = await mailpit.WaitForMessageAsync(oldEmail);
        await Assert.That(notice).IsNotNull();
        await Assert.That(notice!.To?.Select(c => c.Address)).Contains(oldEmail);

        var noticeFull = await mailpit.GetMessageAsync(notice.Id);
        await Assert.That(noticeFull).IsNotNull();
        await Assert.That(noticeFull!.Html).Contains(newEmail);

        // The notice must not contain a verification link — it's informational only.
        await Assert.That(noticeFull.Html).DoesNotContain("token=");
    }

    [Test]
    public async Task ChangeEmail_Unchanged_Returns400_AndSendsNoEmail()
    {
        var email = TestHelper.UniqueEmail("mail-chgemail-unchanged");
        var username = TestHelper.UniqueUsername("mailchgemailunchanged");
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, email, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // No emails at all — neither verification nor notice.
        var any = await mailpit.WaitForMessageAsync(email, TimeSpan.FromSeconds(2));
        await Assert.That(any).IsNull();
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

    [Test]
    public async Task ChangeEmailFlow_NewerRequest_SupersedesOlder_OnlyNewestVerificationDelivered()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-old");
        var firstNewEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-first");
        var secondNewEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-second");
        var username = TestHelper.UniqueUsername("mailchgemailsibling");
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, oldEmail, password);

        // Seed two pending email-change requests for the same user, sharing one coalesce key, committed
        // together so both are pending before the delivery job runs. Newest-wins coalescing must deliver
        // only the newer verification (to secondNewEmail) and skip the older (to firstNewEmail).
        Guid olderOutboxId, newerOutboxId;
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var stamp = await db.Users.Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstAsync();

            var older = NewPendingEmailChange(userId, oldEmail, firstNewEmail, username, stamp);
            var newer = NewPendingEmailChange(userId, oldEmail, secondNewEmail, username, stamp);

            db.UserEmailChanges.AddRange(older.Change, newer.Change);
            db.EmailOutbox.AddRange(older.Outbox, newer.Outbox);
            await db.SaveChangesAsync();

            olderOutboxId = older.Outbox.Id;
            newerOutboxId = newer.Outbox.Id;
        }

        // Only the newest request's verification email is delivered; the older is skipped as superseded.
        var message = await mailpit.WaitForMessageAsync(secondNewEmail);
        await Assert.That(message).IsNotNull();

        var newerOutbox = await WaitForOutboxStatusAsync(newerOutboxId, EmailStatus.Sent);
        var olderOutbox = await WaitForOutboxStatusAsync(olderOutboxId, EmailStatus.Skipped);
        await Assert.That(newerOutbox.Status).IsEqualTo(EmailStatus.Sent);
        await Assert.That(olderOutbox.Status).IsEqualTo(EmailStatus.Skipped);

        // The superseded request's address never receives anything.
        var firstInbox = await mailpit.SearchByRecipientAsync(firstNewEmail);
        await Assert.That(firstInbox.Count).IsEqualTo(0);

        // The delivered (newer) verification completes, switching the email to secondNewEmail.
        var full = await mailpit.GetMessageAsync(message!.Id);
        var token = ExtractQueryParam(full!.Html, "token");
        await Assert.That(token).IsNotNull().And.IsNotEmpty();

        using var anonClient = WebApplicationFactory.CreateClient();
        var verify = await anonClient.PostAsync($"/1/account/email-change/verify?token={token}", null);
        await Assert.That(verify.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var afterUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            await Assert.That(afterUser.Email).IsEqualTo(secondNewEmail);
        }
    }

    [Test]
    public async Task PasswordResetFlow_NewerRequest_SupersedesOlder_OnlyNewestDelivered()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-sibling");
        var username = TestHelper.UniqueUsername("mailpwresetsibling");
        const string newPassword = "FreshPassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        // Seed two pending password-reset requests for the same user, sharing one coalesce key, committed
        // together so both are pending before the delivery job runs. The older must be superseded.
        Guid olderResetId, newerResetId, olderOutboxId, newerOutboxId;
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var stamp = await db.Users.Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstAsync();

            var older = NewPendingReset(userId, email, username, stamp);
            var newer = NewPendingReset(userId, email, username, stamp);

            db.UserPasswordResets.AddRange(older.Reset, newer.Reset);
            db.EmailOutbox.AddRange(older.Outbox, newer.Outbox);
            await db.SaveChangesAsync();

            olderResetId = older.Reset.Id;
            newerResetId = newer.Reset.Id;
            olderOutboxId = older.Outbox.Id;
            newerOutboxId = newer.Outbox.Id;
        }

        // Only the newest request's email is delivered; the older is skipped as superseded.
        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();

        var newerOutbox = await WaitForOutboxStatusAsync(newerOutboxId, EmailStatus.Sent);
        var olderOutbox = await WaitForOutboxStatusAsync(olderOutboxId, EmailStatus.Skipped);
        await Assert.That(newerOutbox.Status).IsEqualTo(EmailStatus.Sent);
        await Assert.That(olderOutbox.Status).IsEqualTo(EmailStatus.Skipped);

        var delivered = await mailpit.SearchByRecipientAsync(email);
        await Assert.That(delivered.Count).IsEqualTo(1);

        // The delivered link belongs to the newer reset, and it completes.
        var full = await mailpit.GetMessageAsync(message!.Id);
        var (resetId, secret) = ExtractPasswordResetParams(full!.Html);
        await Assert.That(resetId).IsEqualTo(newerResetId.ToString());

        using var client = WebApplicationFactory.CreateClient();
        var complete = await client.PostAsync(
            $"/1/account/password-reset/{resetId}/{secret}/complete",
            TestHelper.JsonContent(new { password = newPassword }));
        await Assert.That(complete.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Completing the surviving reset rotated the security stamp, so the superseded sibling is dead
        // at redemption too (its SecurityStampAtCreate no longer matches) - not just undelivered.
        var completeOlder = await client.PostAsync(
            $"/1/account/password-reset/{olderResetId}/irrelevant-secret/complete",
            TestHelper.JsonContent(new { password = "AnotherPassword789#" }));
        await Assert.That(completeOlder.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        // The new password from the surviving reset works.
        var loginResponse = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = newPassword,
            turnstileResponse = "valid-token"
        }));
        await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
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

    // --- Helpers ---

    /// <summary>
    /// Builds a pending <see cref="UserPasswordReset"/> and its matching outbox row (carrying
    /// <paramref name="coalesceKey"/>), exactly as the API would, for direct DB seeding. Each call mints
    /// fresh UUIDv7 ids, so calling it twice in order yields a strictly-older then strictly-newer pair.
    /// </summary>
    private static (UserPasswordReset Reset, EmailOutboxMessage Outbox) NewPendingReset(
        Guid userId, string email, string? recipientName, Guid stamp)
    {
        var reset = new UserPasswordReset
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = HashingUtils.HashToken(CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength)),
            SecurityStampAtCreate = stamp,
            CreatedAt = DateTime.UtcNow
        };
        var outbox = EmailOutboxMessage.ForPasswordReset(reset.Id, userId, email, recipientName);
        return (reset, outbox);
    }

    /// <summary>
    /// Builds a pending <see cref="UserEmailChange"/> and its matching verification outbox row (carrying
    /// <paramref name="coalesceKey"/>) for direct DB seeding. Ordering mirrors <see cref="NewPendingReset"/>.
    /// </summary>
    private static (UserEmailChange Change, EmailOutboxMessage Outbox) NewPendingEmailChange(
        Guid userId, string oldEmail, string newEmail, string? recipientName, Guid stamp)
    {
        var change = new UserEmailChange
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            OldEmail = oldEmail,
            NewEmail = newEmail,
            TokenHash = HashingUtils.HashToken(CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength)),
            SecurityStampAtCreate = stamp,
            CreatedAt = DateTime.UtcNow
        };
        var outbox = EmailOutboxMessage.ForEmailVerification(change.Id, userId, newEmail, recipientName);
        return (change, outbox);
    }

    /// <summary>
    /// Polls the outbox row until it reaches <paramref name="status"/> or the timeout elapses, returning
    /// the last-read row either way so the caller's assertion reports the actual state.
    /// </summary>
    private async Task<EmailOutboxMessage> WaitForOutboxStatusAsync(Guid outboxId, EmailStatus status, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        EmailOutboxMessage? row = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            row = await db.EmailOutbox.AsNoTracking().FirstOrDefaultAsync(m => m.Id == outboxId);
            if (row is not null && row.Status == status) return row;
            await Task.Delay(200);
        }
        return row ?? throw new InvalidOperationException($"Outbox row {outboxId} not found");
    }

    /// <summary>
    /// Extracts a query parameter value from a URL embedded in HTML (first &lt;a href&gt; containing the param).
    /// </summary>
    private static string? ExtractQueryParam(string html, string paramName)
    {
        var hrefMatch = HrefRegex().Match(html);
        while (hrefMatch.Success)
        {
            var href = hrefMatch.Groups[1].Value;
            if (Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var value = query[paramName];
                if (value is not null) return value;
            }
            hrefMatch = hrefMatch.NextMatch();
        }
        return null;
    }

    /// <summary>
    /// Extracts the (passwordResetId, secret) pair from the password-reset URL embedded in email HTML.
    /// URL pattern: /account/password/recover/{guid}/{secret}
    /// </summary>
    private static (string? ResetId, string? Secret) ExtractPasswordResetParams(string html)
    {
        var match = PasswordResetPathRegex().Match(html);
        if (!match.Success) return (null, null);
        return (match.Groups[1].Value, match.Groups[2].Value);
    }

    [GeneratedRegex(@"href=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"/account/password/recover/([0-9a-fA-F\-]+)/([A-Za-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordResetPathRegex();
}
