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
