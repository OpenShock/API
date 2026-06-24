using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Email.Mailjet.Mail;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services.Email.Queue;

/// <summary>
/// Processes due <see cref="QueuedEmail"/> rows: for each, regenerates a fresh token (where applicable)
/// and resends through the raw <see cref="IEmailSender"/>. On success the row is deleted; on a transient
/// failure it is rescheduled with exponential backoff; on a permanent failure or once the attempt limit
/// is reached it is dropped.
///
/// Scoped — the background worker resolves a fresh instance per scan. The raw sender is used directly so
/// a re-send failure reschedules the existing row rather than enqueueing a duplicate.
/// </summary>
public sealed class EmailQueueProcessor
{
    private const int MaxErrorLength = 1000;

    private enum DispatchOutcome
    {
        Sent,
        Dropped
    }

    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IEmailSender _sender;
    private readonly FrontendOptions _frontendOptions;
    private readonly EmailQueueOptions _options;
    private readonly ILogger<EmailQueueProcessor> _logger;

    public EmailQueueProcessor(
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IEmailSender sender,
        FrontendOptions frontendOptions,
        EmailQueueOptions options,
        ILogger<EmailQueueProcessor> logger)
    {
        _dbContextFactory = dbContextFactory;
        _sender = sender;
        _frontendOptions = frontendOptions;
        _options = options;
        _logger = logger;
    }

    public async Task ProcessDueItemsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var due = await db.QueuedEmails
            .Where(q => q.NextAttemptAt <= now)
            .OrderBy(q => q.NextAttemptAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0) return;

        _logger.LogDebug("Processing {Count} due queued email(s)", due.Count);

        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessItemAsync(db, item, cancellationToken);
        }
    }

    private async Task ProcessItemAsync(OpenShockContext db, QueuedEmail item, CancellationToken cancellationToken)
    {
        item.Attempts++;

        try
        {
            var outcome = await DispatchAsync(db, item, cancellationToken);

            db.QueuedEmails.Remove(item);
            if (outcome == DispatchOutcome.Dropped)
                _logger.LogInformation("Dropping queued {EmailType} email {Id}: target no longer needs it", item.Type, item.Id);
            else
                _logger.LogDebug("Sent queued {EmailType} email {Id} on attempt {Attempts}", item.Type, item.Id, item.Attempts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (EmailDeliveryException ex) when (!ex.IsTransient)
        {
            db.QueuedEmails.Remove(item);
            _logger.LogError(ex, "Permanent failure retrying queued {EmailType} email {Id}; dropping", item.Type, item.Id);
        }
        catch (Exception ex)
        {
            if (item.Attempts >= _options.MaxAttempts)
            {
                db.QueuedEmails.Remove(item);
                _logger.LogError(ex, "Giving up on queued {EmailType} email {Id} after {Attempts} attempts", item.Type, item.Id, item.Attempts);
            }
            else
            {
                item.NextAttemptAt = DateTime.UtcNow + _options.GetRetryDelay(item.Attempts);
                item.LastError = Truncate(ex.Message);
                _logger.LogWarning(ex, "Retry {Attempts} failed for queued {EmailType} email {Id}; next attempt at {NextAttemptAt:o}",
                    item.Attempts, item.Type, item.Id, item.NextAttemptAt);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<DispatchOutcome> DispatchAsync(OpenShockContext db, QueuedEmail item, CancellationToken cancellationToken)
        => item.Type switch
        {
            QueuedEmailType.AccountActivation => DispatchActivationAsync(db, item, cancellationToken),
            QueuedEmailType.EmailVerification => DispatchEmailVerificationAsync(db, item, cancellationToken),
            QueuedEmailType.EmailChangeNotice => DispatchEmailChangeNoticeAsync(db, item, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Type, "Unknown queued email type")
        };

    private async Task<DispatchOutcome> DispatchActivationAsync(OpenShockContext db, QueuedEmail item, CancellationToken cancellationToken)
    {
        var payload = item.Payload.RootElement.Deserialize<QueuedEmailPayloads.Activation>()
                      ?? throw new InvalidOperationException("Activation payload could not be deserialized");

        var user = await db.Users
            .Include(u => u.UserDeactivation)
            .Include(u => u.UserActivationRequest)
            .FirstOrDefaultAsync(u => u.Id == payload.UserId, cancellationToken);

        // Nothing to do if the account is gone, already activated, or deactivated.
        if (user is null || user.ActivatedAt is not null || user.UserDeactivation is not null)
            return DispatchOutcome.Dropped;

        // Mint a fresh token and persist it before sending so the link in the email is the stored one.
        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);
        if (user.UserActivationRequest is null)
        {
            user.UserActivationRequest = new UserActivationRequest
            {
                UserId = user.Id,
                TokenHash = HashingUtils.HashToken(token),
                EmailSendAttempts = 1
            };
        }
        else
        {
            user.UserActivationRequest.TokenHash = HashingUtils.HashToken(token);
            user.UserActivationRequest.EmailSendAttempts++;
        }

        await db.SaveChangesAsync(cancellationToken);

        await _sender.ActivateAccount(new Contact(payload.Email, user.Name),
            new Uri(_frontendOptions.BaseUrl, $"/activate?token={token}"), cancellationToken);

        return DispatchOutcome.Sent;
    }

    private async Task<DispatchOutcome> DispatchEmailVerificationAsync(OpenShockContext db, QueuedEmail item, CancellationToken cancellationToken)
    {
        var payload = item.Payload.RootElement.Deserialize<QueuedEmailPayloads.EmailVerification>()
                      ?? throw new InvalidOperationException("Email verification payload could not be deserialized");

        var validSince = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;

        // Only resend for a still-pending, unexpired change whose security stamp hasn't rotated.
        var change = await db.UserEmailChanges
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == payload.UserId && c.NewEmail == payload.Email
                && c.UsedAt == null && c.CreatedAt >= validSince
                && c.SecurityStampAtCreate == c.User.SecurityStamp
                && c.User.UserDeactivation == null && c.User.ActivatedAt != null, cancellationToken);

        if (change is null) return DispatchOutcome.Dropped;

        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);
        change.TokenHash = HashingUtils.HashToken(token);

        await db.SaveChangesAsync(cancellationToken);

        await _sender.VerifyEmail(new Contact(payload.Email, change.User.Name),
            new Uri(_frontendOptions.BaseUrl, $"/verify-email?token={token}"), cancellationToken);

        return DispatchOutcome.Sent;
    }

    private async Task<DispatchOutcome> DispatchEmailChangeNoticeAsync(OpenShockContext db, QueuedEmail item, CancellationToken cancellationToken)
    {
        var payload = item.Payload.RootElement.Deserialize<QueuedEmailPayloads.EmailChangeNotice>()
                      ?? throw new InvalidOperationException("Email change notice payload could not be deserialized");

        // The notice carries no token; it's purely informational, so we just resend it as-is.
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == payload.UserId, cancellationToken);
        if (user is null) return DispatchOutcome.Dropped;

        await _sender.EmailChangeNotice(new Contact(payload.Email, user.Name), payload.NewEmail, cancellationToken);

        return DispatchOutcome.Sent;
    }

    private static string Truncate(string value)
        => value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
