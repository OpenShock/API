using System.Net;
using System.Text.Json;

using OpenShock.Common.JsonSerialization;

namespace OpenShock.Common.Tests.JsonSerialization;

public class IPAddressJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = JsonOptions.Default;

    [Test]
    [Arguments("127.0.0.1")]
    [Arguments("8.8.4.4")]
    [Arguments("::1")]
    [Arguments("2001:db8::ff00:42:8329")]
    public async Task Serialize_WritesTextualForm(string address)
    {
        var json = JsonSerializer.Serialize(IPAddress.Parse(address), Options);

        await Assert.That(json).IsEqualTo($"\"{address}\"");
    }

    [Test]
    public async Task Serialize_Property_WritesString()
    {
        var json = JsonSerializer.Serialize(new Holder { Ip = IPAddress.Loopback }, Options);

        await Assert.That(json).IsEqualTo("""{"ip":"127.0.0.1"}""");
    }

    [Test]
    [Arguments("127.0.0.1")]
    [Arguments("::1")]
    public async Task Deserialize_ParsesTextualForm(string address)
    {
        var ip = JsonSerializer.Deserialize<IPAddress>($"\"{address}\"", Options);

        await Assert.That(ip).IsEqualTo(IPAddress.Parse(address));
    }

    [Test]
    [Arguments("\"not an ip\"")]
    [Arguments("{\"addressFamily\":\"InterNetwork\"}")]
    [Arguments("1")]
    public async Task Deserialize_Throws_WhenNotAnAddressString(string json)
    {
        await Assert.That(() => JsonSerializer.Deserialize<IPAddress>(json, Options)).Throws<JsonException>();
    }

    private sealed class Holder
    {
        public required IPAddress Ip { get; init; }
    }
}
