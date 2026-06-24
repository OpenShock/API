using OpenShock.Common.Options;
using OpenShock.Common.Services.Email.Mailjet.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.Common.Services.Email.Mailjet;

public sealed class MailjetEmailService : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailServiceTemplates _templates;
    private readonly MailOptions.MailSenderContact _sender;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(
            HttpClient httpClient,
            EmailServiceTemplates templates,
            MailOptions.MailSenderContact sender,
            ILogger<MailjetEmailService> logger
        )
    {
        _httpClient = httpClient;
        _templates = templates;
        _sender = sender;
        _logger = logger;
    }

    #region Interface methods

    public async Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.AccountActivation.RenderAsync(new { To = to, ActivationLink = activationLink });
        await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.PasswordReset.RenderAsync(new { To = to, ResetLink = resetLink });
        await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.EmailVerification.RenderAsync(new { To = to, VerifyLink = verificationLink });
        await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.EmailChangeNotice.RenderAsync(new { To = to, NewEmail = newEmail });
        await SendMail(to, subject, htmlBody, cancellationToken);
    }

    #endregion

    private Task SendMail(Contact to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendMails([new DirectMail { From = _sender, To = [to], Subject = subject, HTMLPart = htmlBody }], cancellationToken);

    private async Task SendMails(DirectMail[] mails, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Sending mails {@Mails}", mails);

        var json = JsonSerializer.Serialize(new MailsWrap { Messages = mails }, JsonOptions.Default);

        HttpResponseMessage response;
        try
        {
            using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            response = await _httpClient.PostAsync("send", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Connection-level failure (DNS, refused, reset, TLS). Always worth retrying.
            throw new EmailDeliveryException(isTransient: true, "Failed to reach the Mailjet API", ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent mail");
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error sending mails. Got unsuccessful status code {StatusCode} for mails {@Mails} with error body {Body}",
                response.StatusCode, mails, body);

            // Retry only on rate limiting (429) and server-side errors (5xx). Other 4xx (bad request,
            // auth, rejected recipient) are permanent and re-sending would just fail again.
            var statusCode = (int)response.StatusCode;
            var isTransient = statusCode == 429 || statusCode >= 500;
            throw new EmailDeliveryException(isTransient, $"Mailjet returned status code {statusCode}");
        }
    }
}
