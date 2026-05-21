using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

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
    public async Task V1PasswordReset_SendsPasswordResetEmail()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset");
        var username = TestHelper.UniqueUsername("mailpwreset");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        await Assert.That(message!.To?.Select(c => c.Address)).Contains(email);
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
        var resetResponse = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
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
        var loginResponse = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email,
            password = newPassword
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
    public async Task PasswordResetComplete_LegacyRecoverRoute_StillWorks()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-legacy");
        var username = TestHelper.UniqueUsername("mailpwresetlegacy");
        const string newPassword = "LegacyNewPassword456#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var resetResponse = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(resetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        var fullMessage = await mailpit.GetMessageAsync(message!.Id);
        var (resetId, secret) = ExtractPasswordResetParams(fullMessage!.Html);
        await Assert.That(resetId).IsNotNull().And.IsNotEmpty();

        // Hit the deprecated route directly — must still complete the reset.
        var completeResponse = await client.PostAsync(
            $"/1/account/recover/{resetId}/{secret}",
            TestHelper.JsonContent(new { password = newPassword }));
        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var loginResponse = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email,
            password = newPassword
        }));
        await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task PasswordResetCheck_LegacyHeadRecoverRoute_StillWorks()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-check-legacy");
        var username = TestHelper.UniqueUsername("mailpwresetchecklegacy");
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var resetResponse = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(resetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        var fullMessage = await mailpit.GetMessageAsync(message!.Id);
        var (resetId, secret) = ExtractPasswordResetParams(fullMessage!.Html);

        var legacyCheck = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Head, $"/1/account/recover/{resetId}/{secret}"));
        await Assert.That(legacyCheck.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task PasswordResetCheck_InvalidToken_Returns404()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-check-invalid");
        var username = TestHelper.UniqueUsername("mailpwresetcheckinvalid");
        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var bogusId = Guid.NewGuid();
        const string bogusSecret = "thisisnotarealtokenatallzz";

        var response = await client.GetAsync($"/1/account/password-reset/{bogusId}/{bogusSecret}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ChangeEmailFlow_SecondPendingRequest_InvalidatedAfterFirstCompletes()
    {
        var oldEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-old");
        var firstNewEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-first");
        var secondNewEmail = TestHelper.UniqueEmail("mail-chgemail-sibling-second");
        var username = TestHelper.UniqueUsername("mailchgemailsibling");
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, username, oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Initiate two concurrent email change requests
        var firstInit = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = firstNewEmail
        }));
        await Assert.That(firstInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var secondInit = await client.PostAsync("/1/account/email-change", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = secondNewEmail
        }));
        await Assert.That(secondInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var firstMessage = await mailpit.WaitForMessageAsync(firstNewEmail);
        var secondMessage = await mailpit.WaitForMessageAsync(secondNewEmail);
        await Assert.That(firstMessage).IsNotNull();
        await Assert.That(secondMessage).IsNotNull();

        var firstFull = await mailpit.GetMessageAsync(firstMessage!.Id);
        var secondFull = await mailpit.GetMessageAsync(secondMessage!.Id);
        var firstToken = ExtractQueryParam(firstFull!.Html, "token");
        var secondToken = ExtractQueryParam(secondFull!.Html, "token");
        await Assert.That(firstToken).IsNotNull().And.IsNotEmpty();
        await Assert.That(secondToken).IsNotNull().And.IsNotEmpty();

        using var anonClient = WebApplicationFactory.CreateClient();

        // Complete the first request — email becomes firstNewEmail
        var firstVerify = await anonClient.PostAsync($"/1/account/email-change/verify?token={firstToken}", null);
        await Assert.That(firstVerify.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Second pending request is now invalid: its EmailVersionAtCreate snapshot no longer matches User.EmailVersion.
        var secondVerify = await anonClient.PostAsync($"/1/account/email-change/verify?token={secondToken}", null);
        await Assert.That(secondVerify.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var afterUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        await Assert.That(afterUser.Email).IsEqualTo(firstNewEmail);
    }

    [Test]
    public async Task PasswordResetFlow_SecondPendingResetInvalidatedAfterFirstCompletes()
    {
        var email = TestHelper.UniqueEmail("mail-pwreset-sibling");
        var username = TestHelper.UniqueUsername("mailpwresetsibling");
        const string firstNewPassword = "FirstNewPassword123#";
        const string secondNewPassword = "SecondNewPassword456#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        // Fire two reset requests back-to-back, then wait for both emails to land.
        var firstInit = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(firstInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var secondInit = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(secondInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var messages = await mailpit.WaitForMessagesAsync(email, minCount: 2);
        await Assert.That(messages.Count).IsGreaterThanOrEqualTo(2);

        // Mailpit returns messages in reverse-chronological order; take the two for our recipient.
        var firstFull = await mailpit.GetMessageAsync(messages[1].Id);
        var secondFull = await mailpit.GetMessageAsync(messages[0].Id);
        var (firstResetId, firstSecret) = ExtractPasswordResetParams(firstFull!.Html);
        var (secondResetId, secondSecret) = ExtractPasswordResetParams(secondFull!.Html);
        await Assert.That(firstResetId).IsNotNull().And.IsNotEmpty();
        await Assert.That(secondResetId).IsNotNull().And.IsNotEmpty();

        // Complete the first reset
        var firstComplete = await client.PostAsync(
            $"/1/account/password-reset/{firstResetId}/{firstSecret}/complete",
            TestHelper.JsonContent(new { password = firstNewPassword }));
        await Assert.That(firstComplete.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // The second (sibling) reset must no longer be usable
        var secondCheck = await client.GetAsync(
            $"/1/account/password-reset/{secondResetId}/{secondSecret}");
        await Assert.That(secondCheck.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var secondComplete = await client.PostAsync(
            $"/1/account/password-reset/{secondResetId}/{secondSecret}/complete",
            TestHelper.JsonContent(new { password = secondNewPassword }));
        await Assert.That(secondComplete.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        // First (new) password still works
        var loginResponse = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email,
            password = firstNewPassword
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
