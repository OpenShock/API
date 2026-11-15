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

    [Test]
    public async Task GetOpenApiDocument_IncludesXmlDocsTheBuiltInSupportLeavesOut()
    {
        using var client = WebApplicationFactory.CreateClient();

        using var doc = JsonDocument.Parse(await client.GetStringAsync("/openapi.json"));
        var root = doc.RootElement;

        // Every tag is described by the <summary> of its controller
        var undescribedTags = root.GetProperty("tags").EnumerateArray()
            .Where(tag => !tag.TryGetProperty("description", out var description) || string.IsNullOrWhiteSpace(description.GetString()))
            .Select(tag => tag.GetProperty("name").GetString())
            .ToArray();
        await Assert.That(undescribedTags).IsEmpty();

        // A documented <response code="500"> without a [ProducesResponseType]
        var linkShareCode = root.GetProperty("paths").GetProperty("/1/shares/code/{shareCodeId}").GetProperty("post");
        var description = linkShareCode.GetProperty("responses").GetProperty("500").GetProperty("description").GetString();
        await Assert.That(description).IsEqualTo("Error while linking share code to your account");
    }
}
