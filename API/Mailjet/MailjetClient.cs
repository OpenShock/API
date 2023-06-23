using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using ShockLink.API.Mailjet.Mail;

namespace ShockLink.API.Mailjet;

public class MailjetClient : IMailjetClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MailjetClient> _logger;

    public MailjetClient(HttpClient httpClient, ILogger<MailjetClient> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public Task SendMail(MailBase templateMail) => SendMails(new[] { templateMail });

    public async Task SendMails(IEnumerable<MailBase> mails)
    {
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Sending mails {@Mails}", mails);

        var response = await _httpClient.PostAsync("send",
            new StringContent(JsonConvert.SerializeObject(new MailsWrap
            {
                Messages = mails
            }), Encoding.UTF8, MediaTypeNames.Application.Json));
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error sending mails. Got unsuccessful status code {StatusCode} for mails {@Mails} with error body {Body}",
                response.StatusCode, mails, await response.Content.ReadAsStringAsync());
        }
        else _logger.LogDebug("Successfully sent mail");
    }
}