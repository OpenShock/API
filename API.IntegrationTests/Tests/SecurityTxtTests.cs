using System.Net;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class SecurityTxtTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task GetSecurityTxt_ServesConfiguredFileAsPlainText()
    {
        using var client = WebApplicationFactory.CreateClient(new() { BaseAddress = new Uri("https://api.example.org") });

        var response = await client.GetAsync("/.well-known/security.txt");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/plain");
        await Assert.That(response.Content.Headers.ContentType?.CharSet).IsEqualTo("utf-8");

        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo(
            "Contact: mailto:security@example.org\n" +
            "Contact: https://example.org/report\n" +
            "Expires: 2099-01-01T00:00:00Z\n" +
            "Canonical: https://api.example.org/.well-known/security.txt\n" +
            "Policy: https://example.org/policy\n");
    }

    [Test]
    public async Task GetSecurityTxt_OverPlainHttp_OmitsCanonical()
    {
        using var client = WebApplicationFactory.CreateClient(new() { BaseAddress = new Uri("http://api.example.org") });

        var body = await client.GetStringAsync("/.well-known/security.txt");

        await Assert.That(body).DoesNotContain("Canonical:");
    }
}
