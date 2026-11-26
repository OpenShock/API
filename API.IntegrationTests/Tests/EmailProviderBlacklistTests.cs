using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Covers adding to the email provider blacklist: entries are validated up front, so one bad line in a large
/// paste rejects the request instead of failing halfway, and the whole list goes to the database in one insert.
/// </summary>
public sealed class EmailProviderBlacklistTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Add_NormalizesEntriesAndSkipsExisting(CancellationToken cancellationToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var first = $"zqx-{suffix}-a.example";
        var second = $"zqx-{suffix}-b.example";
        using var client = await CreateAdminClient("blacklistadd");

        try
        {
            var response = await client.PostAsync("/1/admin/blacklist/emailProviders", TestHelper.JsonContent(new
            {
                // A full address, different case, a trailing dot, padding, blank lines and a repeat all fold together
                domains = new[] { $"someone@{first.ToUpperInvariant()}", $"  {first}. ", "", "   ", second, second }
            }), cancellationToken);

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
            using (var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)))
            {
                await Assert.That(json.RootElement.GetProperty("submitted").GetInt32()).IsEqualTo(2);
                await Assert.That(json.RootElement.GetProperty("added").GetInt32()).IsEqualTo(2);
            }
            // Adding again only reports the new one
            var third = $"zqx-{suffix}-c.example";
            var again = await client.PostAsync("/1/admin/blacklist/emailProviders",
                TestHelper.JsonContent(new { domains = new[] { first, third } }), cancellationToken);
            await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.OK);
            using (var json = JsonDocument.Parse(await again.Content.ReadAsStringAsync(cancellationToken)))
            {
                await Assert.That(json.RootElement.GetProperty("added").GetInt32()).IsEqualTo(1);
                await Assert.That(json.RootElement.GetProperty("alreadyListed").GetInt32()).IsEqualTo(1);
            }

            await Assert.That(await ListedDomains(suffix)).IsEquivalentTo([first, second, third]);
        }
        finally
        {
            await DeleteDomains(suffix);
        }
    }

    [Test]
    public async Task Add_WithAnyInvalidEntry_AddsNothing(CancellationToken cancellationToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        using var client = await CreateAdminClient("blacklistinvalid");
        string[] invalid = ["localhost", "127.0.0.1", "not a domain", "@", new string('a', 250) + ".example", "under_score.example", "-leading.example", "trailing-.example", "double..dot.example"];

        try
        {
            var response = await client.PostAsync("/1/admin/blacklist/emailProviders", TestHelper.JsonContent(new
            {
                domains = invalid.Prepend($"zqx-{suffix}-valid.example").ToArray()
            }), cancellationToken);

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            await Assert.That(json.RootElement.GetProperty("invalidCount").GetInt32()).IsEqualTo(invalid.Length);
            var echoed = json.RootElement.GetProperty("invalidEntries").EnumerateArray().Select(x => x.GetString()!).ToArray();
            await Assert.That(echoed).IsEquivalentTo(invalid);

            await Assert.That(await ListedDomains(suffix)).IsEmpty();
        }
        finally
        {
            await DeleteDomains(suffix);
        }
    }

    [Test]
    public async Task Add_TensOfThousandsInOneRequest(CancellationToken cancellationToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        using var client = await CreateAdminClient("blacklistbulk");
        var domains = Enumerable.Range(0, 50_000).Select(i => $"zqx-{suffix}-{i}.example").ToArray();

        try
        {
            var response = await client.PostAsync("/1/admin/blacklist/emailProviders",
                TestHelper.JsonContent(new { domains }), cancellationToken);

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            await Assert.That(json.RootElement.GetProperty("added").GetInt32()).IsEqualTo(domains.Length);
        }
        finally
        {
            await DeleteDomains(suffix);
        }
    }

    private async Task<string[]> ListedDomains(string suffix)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var prefix = $"zqx-{suffix}-";
        return await db.EmailProviderBlacklists.Where(x => x.Domain.StartsWith(prefix)).Select(x => x.Domain).ToArrayAsync();
    }

    private async Task DeleteDomains(string suffix)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var prefix = $"zqx-{suffix}-";
        await db.EmailProviderBlacklists.Where(x => x.Domain.StartsWith(prefix)).ExecuteDeleteAsync();
    }

    private async Task<HttpClient> CreateAdminClient(string prefix)
    {
        var admin = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername(prefix), TestHelper.UniqueEmail(prefix), "SecurePassword123#");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == admin.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Roles, [RoleType.Admin]));

        return TestHelper.CreateAuthenticatedClient(WebApplicationFactory, admin.SessionToken);
    }
}
