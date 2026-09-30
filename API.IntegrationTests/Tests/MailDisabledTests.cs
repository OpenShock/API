using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// With no mail provider configured no activation link can ever be delivered, so signup must activate
/// the account outright instead of parking it behind an activation flow it can never complete.
/// </summary>
public sealed class MailDisabledTests
{
    [ClassDataSource<MailDisabledWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required MailDisabledWebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task V2Signup_MailDisabled_ActivatesAccountAndEnqueuesNoEmail()
    {
        using var client = WebApplicationFactory.CreateClient();
        var email = TestHelper.UniqueEmail("mail-disabled");

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = "mailDisabledUser",
            password = "SecurePassword123#",
            email,
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var user = await db.Users
            .Include(u => u.UserActivationRequest)
            .FirstOrDefaultAsync(u => u.Email == email);

        await Assert.That(user).IsNotNull();
        await Assert.That(user!.ActivatedAt).IsNotNull();
        await Assert.That(user.UserActivationRequest).IsNull();

        var activationMails = await db.EmailOutbox
            .CountAsync(m => m.Recipient == email && m.Type == EmailType.AccountActivation);
        await Assert.That(activationMails).IsEqualTo(0);
    }

    [Test, DependsOn(nameof(V2Signup_MailDisabled_ActivatesAccountAndEnqueuesNoEmail))]
    public async Task V2Signup_MailDisabled_AccountCanLogInImmediately()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "mailDisabledUser",
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task BackendInfo_MailDisabled_ReportsMailDisabled()
    {
        using var client = WebApplicationFactory.CreateClient();

        var json = await client.GetStringAsync("/1");
        using var doc = JsonDocument.Parse(json);

        var isMailEnabled = doc.RootElement.GetProperty("data").GetProperty("isMailEnabled").GetBoolean();
        await Assert.That(isMailEnabled).IsFalse();
    }
}
