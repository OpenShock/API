using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Email;
using OpenShock.Common.Services.Email.Mailjet.Mail;
using OpenShock.Common.Services.Email.Queue;

namespace OpenShock.API.Services.Email.Queue;

/// <summary>
/// <see cref="IEmailService"/> decorator implementing the send-now / queue-on-failure behaviour.
/// It hands the email straight to the underlying <see cref="IEmailSender"/>; if delivery fails
/// transiently it records a minimal, token-free <see cref="QueuedEmail"/> for the background worker to
/// retry. Permanent failures are logged and dropped. The caller's action never fails because of mail.
/// </summary>
public sealed class QueueingEmailService : IEmailService
{
    private const int MaxErrorLength = 1000;

    private readonly IEmailSender _sender;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly EmailQueueOptions _options;
    private readonly ILogger<QueueingEmailService> _logger;

    public QueueingEmailService(
        IEmailSender sender,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        EmailQueueOptions options,
        ILogger<QueueingEmailService> logger)
    {
        _sender = sender;
        _dbContextFactory = dbContextFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ActivateAccount(Guid userId, Contact to, Uri activationLink, CancellationToken cancellationToken = default)
        => SendOrQueue(
            QueuedEmailType.AccountActivation,
            new QueuedEmailPayloads.Activation(userId, to.Email),
            () => _sender.ActivateAccount(to, activationLink, cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public async Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        // Password reset is intentionally not queued: a failed reset can simply be re-requested by the
        // user, and we don't want a background job minting fresh reset tokens. Swallow + log instead.
        try
        {
            await _sender.PasswordReset(to, resetLink, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email; not retrying");
        }
    }

    /// <inheritdoc />
    public Task VerifyEmail(Guid userId, Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
        => SendOrQueue(
            QueuedEmailType.EmailVerification,
            new QueuedEmailPayloads.EmailVerification(userId, to.Email),
            () => _sender.VerifyEmail(to, verificationLink, cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public Task EmailChangeNotice(Guid userId, Contact to, string newEmail, CancellationToken cancellationToken = default)
        => SendOrQueue(
            QueuedEmailType.EmailChangeNotice,
            new QueuedEmailPayloads.EmailChangeNotice(userId, to.Email, newEmail),
            () => _sender.EmailChangeNotice(to, newEmail, cancellationToken),
            cancellationToken);

    private async Task SendOrQueue(QueuedEmailType type, object payload, Func<Task> send, CancellationToken cancellationToken)
    {
        try
        {
            await send();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (EmailDeliveryException ex) when (!ex.IsTransient)
        {
            // Permanent provider rejection — retrying would just fail again.
            _logger.LogError(ex, "Permanent failure sending {EmailType} email; dropping", type);
        }
        catch (Exception ex)
        {
            // Transient EmailDeliveryException or any unexpected error — queue for retry.
            _logger.LogWarning(ex, "Failed to send {EmailType} email; queueing for retry", type);
            await EnqueueAsync(type, payload, ex);
        }
    }

    private async Task EnqueueAsync(QueuedEmailType type, object payload, Exception ex)
    {
        try
        {
            // Use None so the row is persisted even if the originating request is being torn down.
            await using var db = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);

            db.QueuedEmails.Add(new QueuedEmail
            {
                Id = Guid.CreateVersion7(),
                Type = type,
                Payload = JsonSerializer.SerializeToDocument(payload),
                Attempts = 1, // the just-failed send counts as the first attempt
                NextAttemptAt = DateTime.UtcNow + _options.GetRetryDelay(1),
                LastError = Truncate(ex.Message)
            });

            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception enqueueEx)
        {
            // Last resort: the email is lost, but a queue write failure must not crash the caller.
            _logger.LogError(enqueueEx, "Failed to queue {EmailType} email for retry", type);
        }
    }

    private static string Truncate(string value)
        => value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
