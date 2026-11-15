using System.Net;
using System.Text.Json;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class OpenApiDocumentTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task GetOpenApiDocument_ServesSingleDocumentWithEveryVersion()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/openapi.json");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();

        await Assert.That(paths).Contains("/1/account");
        await Assert.That(paths).Contains("/2/account/login");
    }
}
