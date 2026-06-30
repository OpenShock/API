using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Cron.IntegrationTests.Helpers;

/// <summary>
/// Helper for querying the Mailpit HTTP API in integration tests.
/// </summary>
public sealed class MailpitHelper : IDisposable
{
    private readonly HttpClient _client;

    public MailpitHelper(string apiBaseUrl)
    {
        _client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    }

    /// <summary>
    /// Polls until at least one email arrives for the given recipient address, or the timeout elapses.
    /// Returns null if no message arrived within the timeout.
    /// </summary>
    public async Task<MailpitMessage?> WaitForMessageAsync(
        string toAddress,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            var match = (await SearchByRecipientAsync(toAddress, limit: 1, cancellationToken)).FirstOrDefault();
            if (match is not null) return match;
            await Task.Delay(300, cancellationToken);
        }
        return null;
    }

    /// <summary>
    /// Returns all messages currently in Mailpit addressed to the given recipient.
    /// Server-side filtered via the Mailpit search API.
    /// </summary>
    public async Task<List<MailpitMessage>> SearchByRecipientAsync(
        string toAddress,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = Uri.EscapeDataString($"to:{toAddress}");
        var response = await _client.GetFromJsonAsync<MailpitSearchResponse>(
            $"/api/v1/search?query={query}&limit={limit}", cancellationToken);
        return response?.Messages ?? [];
    }

    /// <summary>
    /// Fetches the full HTML body of a message by its ID.
    /// </summary>
    public async Task<MailpitFullMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _client.GetFromJsonAsync<MailpitFullMessage>(
            $"/api/v1/message/{messageId}",
            cancellationToken);
    }

    public void Dispose() => _client.Dispose();

    // --- DTOs ---

    public sealed class MailpitSearchResponse
    {
        [JsonPropertyName("messages")]
        public List<MailpitMessage> Messages { get; init; } = [];
    }

    public sealed class MailpitMessage
    {
        [JsonPropertyName("ID")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("Subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("To")]
        public List<MailpitContact>? To { get; init; }

        [JsonPropertyName("Snippet")]
        public string Snippet { get; init; } = string.Empty;
    }

    public sealed class MailpitFullMessage
    {
        [JsonPropertyName("ID")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("HTML")]
        public string Html { get; init; } = string.Empty;

        [JsonPropertyName("Text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("To")]
        public List<MailpitContact>? To { get; init; }
    }

    public sealed class MailpitContact
    {
        [JsonPropertyName("Name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("Address")]
        public string Address { get; init; } = string.Empty;
    }
}
