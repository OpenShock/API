using OpenShock.Cron.Options;
using OpenShock.Cron.Services.Email.Mailjet.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.Cron.Services.Email.Mailjet;

public sealed class MailjetEmailService : IEmailService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly EmailServiceTemplates _templates;
    private readonly MailSenderContact _sender;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(
            HttpClient httpClient,
            EmailServiceTemplates templates,
            MailSenderContact sender,
            ILogger<MailjetEmailService> logger
        )
    {
        _httpClient = httpClient;
        _templates = templates;
        _sender = sender;
        _logger = logger;
    }

    #region Interface methods

    public async Task<EmailSendResult> ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.AccountActivation.RenderAsync(new { To = to, ActivationLink = activationLink });
        return await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.PasswordReset.RenderAsync(new { To = to, ResetLink = resetLink });
        return await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.EmailVerification.RenderAsync(new { To = to, VerifyLink = verificationLink });
        return await SendMail(to, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = await _templates.EmailChangeNotice.RenderAsync(new { To = to, NewEmail = newEmail });
        return await SendMail(to, subject, htmlBody, cancellationToken);
    }

    #endregion

    private Task<EmailSendResult> SendMail(Contact to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendMails([new DirectMail { From = _sender, To = [to], Subject = subject, HTMLPart = htmlBody }], cancellationToken);

    private async Task<EmailSendResult> SendMails(DirectMail[] mails, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Sending mails {@Mails}", mails);

        var json = JsonSerializer.Serialize(new MailsWrap { Messages = mails }, JsonOptions.Default);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("send",
                new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Network failure or request timeout - worth retrying.
            _logger.LogWarning(ex, "Transient failure sending mails {@Mails}", mails);
            return EmailSendResult.TransientFailure;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Successfully sent mail");
            return EmailSendResult.Sent;
        }

        var statusCode = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Error sending mails. Got unsuccessful status code {StatusCode} for mails {@Mails} with error body {Body}",
            response.StatusCode, mails, body);

        // 429 (rate limited) and 5xx (provider-side) are temporary; other 4xx are permanent.
        return statusCode is 429 or >= 500 and <= 599
            ? EmailSendResult.TransientFailure
            : EmailSendResult.PermanentFailure;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
