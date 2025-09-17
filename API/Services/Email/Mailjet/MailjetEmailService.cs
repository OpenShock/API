using OpenShock.API.Options;
using OpenShock.API.Services.Email.Mailjet.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.API.Services.Email.Mailjet;

public sealed class MailjetEmailService : IEmailService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly MailJetOptions _options;
    private readonly MailOptions.MailSenderContact _sender;
    private readonly ILogger<MailjetEmailService> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="options"></param>
    /// <param name="sender"></param>
    /// <param name="logger"></param>
    public MailjetEmailService(
            HttpClient httpClient,
            MailJetOptions options,
            MailOptions.MailSenderContact sender,
            ILogger<MailjetEmailService> logger
        )
    {
        _httpClient = httpClient;
        _options = options;
        _sender = sender;
        _logger = logger;
    }

    #region Interface methods

    /// <inheritdoc />
    public async Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        await SendMail(new TemplateMail
        {
            From = _sender,
            Subject = "Password reset request",
            To = [to],
            TemplateId = _options.Template.PasswordReset,
            Variables = new Dictionary<string, string>
            {
                {"link", resetLink.ToString() },
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task VerifyEmail(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        await SendMail(new TemplateMail
        {
            From = _sender,
            Subject = "Verify your Email Address",
            To = [to],
            TemplateId = _options.Template.VerifyEmail,
            Variables = new Dictionary<string, string>
            {
                {"link", activationLink.ToString() },
            }
        }, cancellationToken);
    }

    #endregion

    private Task SendMail(MailBase templateMail, CancellationToken cancellationToken = default) => SendMails([templateMail], cancellationToken);

    private async Task SendMails(MailBase[] mails, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Sending mails {@Mails}", mails);

        var json = JsonSerializer.Serialize(new MailsWrap
        {
            Messages = mails
        }, JsonSettings.CamelCase);

        var response = await _httpClient.PostAsync("send",
            new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error sending mails. Got unsuccessful status code {StatusCode} for mails {@Mails} with error body {Body}",
                response.StatusCode, mails, await response.Content.ReadAsStringAsync(cancellationToken));
        }
        else _logger.LogDebug("Successfully sent mail");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}