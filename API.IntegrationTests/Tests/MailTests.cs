using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Tests that verify emails are actually delivered via SMTP to Mailpit.
/// Each test uses a unique email address so messages can be filtered by recipient.
/// </summary>
public sealed partial class MailTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Account Activation ---

    [Test]
    public async Task V2Signup_SendsAccountActivationEmail()
    {
        const string email = "mail-activation@test.org";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "mailactivationuser",
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
        const string email = "mail-activate-flow@test.org";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();
        using var client = WebApplicationFactory.CreateClient();

        // Sign up — this triggers an activation email
        var signupResponse = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "mailactivateflowuser",
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
        const string email = "mail-pwreset@test.org";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, "mailpwresetuser", email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new
        {
            email
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();
        await Assert.That(message!.To?.Select(c => c.Address)).Contains(email);
    }

    [Test]
    public async Task V2PasswordReset_SendsPasswordResetEmail()
    {
        const string email = "mail-pwreset-v2@test.org";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, "mailpwresetv2user", email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();
        var response = await client.PostAsync("/2/account/reset-password", TestHelper.JsonContent(new
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
        const string email = "mail-pwreset-flow@test.org";
        const string newPassword = "NewSecurePassword456#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, "mailpwresetflowuser", email, "OldPassword123#");

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
        var checkResponse = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Head, $"/1/account/recover/{resetId}/{secret}"));
        await Assert.That(checkResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Complete the reset with a new password
        var completeResponse = await client.PostAsync(
            $"/1/account/recover/{resetId}/{secret}",
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
        const string oldEmail = "mail-chgemail-flow@test.org";
        const string newEmail = "mail-chgemail-flow-new@test.org";
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "mailchgemailflow", oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Initiate the email change
        var initiateResponse = await client.PostAsync("/1/account/email", TestHelper.JsonContent(new
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
        var verifyResponse = await anonClient.PostAsync($"/1/account/verify-email?token={token}", null);
        await Assert.That(verifyResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Email is now updated
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            var afterUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            await Assert.That(afterUser.Email).IsEqualTo(newEmail);
        }

        // Re-using the same token must now fail
        var replayResponse = await anonClient.PostAsync($"/1/account/verify-email?token={token}", null);
        await Assert.That(replayResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ChangeEmail_WrongPassword_Returns403_AndSendsNoEmail()
    {
        const string oldEmail = "mail-chgemail-badpwd@test.org";
        const string newEmail = "mail-chgemail-badpwd-new@test.org";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "mailchgemailbadpwd", oldEmail, "CorrectPassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email", TestHelper.JsonContent(new
        {
            currentPassword = "WrongPassword!",
            email = newEmail
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        // No verification email should have been dispatched
        var message = await mailpit.WaitForMessageAsync(newEmail, TimeSpan.FromSeconds(2));
        await Assert.That(message).IsNull();
    }

    [Test]
    public async Task ChangeEmailFlow_SecondPendingRequest_InvalidatedAfterFirstCompletes()
    {
        const string oldEmail = "mail-chgemail-sibling@test.org";
        const string firstNewEmail = "mail-chgemail-sibling-first@test.org";
        const string secondNewEmail = "mail-chgemail-sibling-second@test.org";
        const string password = "SecurePassword123#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "mailchgemailsibling", oldEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Initiate two concurrent email change requests
        var firstInit = await client.PostAsync("/1/account/email", TestHelper.JsonContent(new
        {
            currentPassword = password,
            email = firstNewEmail
        }));
        await Assert.That(firstInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var secondInit = await client.PostAsync("/1/account/email", TestHelper.JsonContent(new
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
        var firstVerify = await anonClient.PostAsync($"/1/account/verify-email?token={firstToken}", null);
        await Assert.That(firstVerify.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Second pending request must now be unusable because its OldEmail no longer matches the user's current email
        var secondVerify = await anonClient.PostAsync($"/1/account/verify-email?token={secondToken}", null);
        await Assert.That(secondVerify.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var afterUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        await Assert.That(afterUser.Email).IsEqualTo(firstNewEmail);
    }

    [Test]
    public async Task PasswordResetFlow_SecondPendingResetInvalidatedAfterFirstCompletes()
    {
        const string email = "mail-pwreset-sibling@test.org";
        const string firstNewPassword = "FirstNewPassword123#";
        const string secondNewPassword = "SecondNewPassword456#";
        using var mailpit = WebApplicationFactory.CreateMailpitHelper();

        await TestHelper.CreateUserInDb(WebApplicationFactory, "mailpwresetsibling", email, "OldPassword123#");

        using var client = WebApplicationFactory.CreateClient();

        // Initiate two password reset requests
        var firstInit = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(firstInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Wait for first message before triggering second so we can distinguish them
        var firstMessage = await mailpit.WaitForMessageAsync(email);
        await Assert.That(firstMessage).IsNotNull();

        var secondInit = await client.PostAsync("/1/account/reset", TestHelper.JsonContent(new { email }));
        await Assert.That(secondInit.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Find the second (newer) message
        MailpitHelper.MailpitMessage? secondMessage = null;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline && secondMessage is null)
        {
            var all = await mailpit.GetAllMessagesAsync();
            secondMessage = all.FirstOrDefault(m =>
                m.Id != firstMessage!.Id &&
                m.To?.Any(t => t.Address.Equals(email, StringComparison.OrdinalIgnoreCase)) == true);
            if (secondMessage is null) await Task.Delay(300);
        }
        await Assert.That(secondMessage).IsNotNull();

        var firstFull = await mailpit.GetMessageAsync(firstMessage!.Id);
        var secondFull = await mailpit.GetMessageAsync(secondMessage!.Id);
        var (firstResetId, firstSecret) = ExtractPasswordResetParams(firstFull!.Html);
        var (secondResetId, secondSecret) = ExtractPasswordResetParams(secondFull!.Html);
        await Assert.That(firstResetId).IsNotNull().And.IsNotEmpty();
        await Assert.That(secondResetId).IsNotNull().And.IsNotEmpty();

        // Complete the first reset
        var firstComplete = await client.PostAsync(
            $"/1/account/recover/{firstResetId}/{firstSecret}",
            TestHelper.JsonContent(new { password = firstNewPassword }));
        await Assert.That(firstComplete.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // The second (sibling) reset must no longer be usable
        var secondCheck = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Head, $"/1/account/recover/{secondResetId}/{secondSecret}"));
        await Assert.That(secondCheck.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var secondComplete = await client.PostAsync(
            $"/1/account/recover/{secondResetId}/{secondSecret}",
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
        const string takenEmail = "mail-chgemail-taken-existing@test.org";
        const string ownEmail = "mail-chgemail-taken-own@test.org";
        const string password = "SecurePassword123#";

        await TestHelper.CreateUserInDb(WebApplicationFactory, "mailchgemailtaken1", takenEmail, password);
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "mailchgemailtaken2", ownEmail, password);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/email", TestHelper.JsonContent(new
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
