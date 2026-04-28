using System.Net;
using System.Text.Json;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class PublicTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Metadata / Version ---

    [Test]
    public async Task GetMetadataV1_ReturnsValidResponse()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("version").GetString()).IsNotNullOrWhiteSpace();
        await Assert.That(data.GetProperty("currentTime").GetDateTimeOffset()).IsBetween(
            DateTimeOffset.UtcNow.AddSeconds(-10),
            DateTimeOffset.UtcNow.AddSeconds(10));
    }

    // --- Public Stats ---

    [Test]
    public async Task GetStats_ReturnsDevicesOnlineCount()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/public/stats");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var devicesOnline = data.GetProperty("devicesOnline").GetInt64();
        await Assert.That(devicesOnline).IsGreaterThanOrEqualTo(0);
    }

    // --- Check Username (public endpoint) ---

    [Test]
    public async Task CheckUsername_Available_ReturnsAvailable()
    {
        using var client = WebApplicationFactory.CreateClient();

        using var content = new StringContent(
            JsonSerializer.Serialize(new { username = "totallyuniquename123" }),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/1/account/username/check", content);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var availability = doc.RootElement.GetProperty("availability").GetString();
        await Assert.That(availability).IsEqualTo("Available");
    }
}
