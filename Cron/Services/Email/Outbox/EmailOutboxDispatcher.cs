using Microsoft.EntityFrameworkCore;
using OpenShock.Cron.Services.Email.Mailjet.Mail;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;

namespace OpenShock.Cron.Services.Email.Outbox;

/// <inheritdoc />
public sealed class EmailOutboxDispatcher : IEmailOutboxDispatcher
{
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<EmailOutboxDispatcher> _logger;

    /// <summary>DI constructor.</summary>
    public EmailOutboxDispatcher(IEmailService emailService, FrontendOptions frontendOptions, ILogger<EmailOutboxDispatcher> logger)
    {
        _emailService = emailService;
        _frontendOptions = frontendOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailDispatchResult> SendAsync(EmailOutboxMessage message, OpenShockContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            return message.Type switch
            {
                EmailType.AccountActivation => await SendAccountActivation(message, db, cancellationToken),
                EmailType.PasswordReset => await SendPasswordReset(message, db, cancellationToken),
                EmailType.EmailVerification => await SendEmailVerification(message, db, cancellationToken),
                EmailType.EmailChangeNotice => await SendEmailChangeNotice(message, cancellationToken),
                _ => EmailDispatchResult.Permanent($"Unknown email type {message.Type}")
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected failure (e.g. database hiccup while loading the request row). Treat as
            // transient so the message is retried rather than dropped.
            _logger.LogError(ex, "Unexpected error dispatching email outbox message {MessageId}", message.Id);
            return EmailDispatchResult.Transient(ex.Message);
        }
    }

    private async Task<EmailDispatchResult> SendAccountActivation(EmailOutboxMessage message, OpenShockContext db, CancellationToken ct)
    {
        if (!TryGetGuid(message, EmailOutboxPayloadKeys.UserId, out var userId))
            return EmailDispatchResult.Permanent($"Payload missing {EmailOutboxPayloadKeys.UserId}");

        var request = await db.UserActivationRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (request is null) return EmailDispatchResult.Skip("Activation request no longer exists");
        if (request.User.ActivatedAt is not null) return EmailDispatchResult.Skip("Account already activated");

        var token = await MintTokenAsync(db, h => request.TokenHash = h, ct);
        var link = new Uri(_frontendOptions.BaseUrl, $"/activate?token={token}");
        return FromSend(await _emailService.ActivateAccount(ToContact(message), link, ct));
    }

    private async Task<EmailDispatchResult> SendPasswordReset(EmailOutboxMessage message, OpenShockContext db, CancellationToken ct)
    {
        if (!TryGetGuid(message, EmailOutboxPayloadKeys.PasswordResetId, out var resetId))
            return EmailDispatchResult.Permanent($"Payload missing {EmailOutboxPayloadKeys.PasswordResetId}");

        var reset = await db.UserPasswordResets
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == resetId, ct);
        if (reset is null) return EmailDispatchResult.Skip("Password reset no longer exists");
        if (ValidatePending("Password reset", reset.UsedAt, reset.CreatedAt, Duration.PasswordResetRequestLifetime,
                reset.SecurityStampAtCreate, reset.User.SecurityStamp) is { } resetSkip) return resetSkip;

        var token = await MintTokenAsync(db, h => reset.TokenHash = h, ct);
        var link = new Uri(_frontendOptions.BaseUrl, $"/#/account/password/recover/{reset.Id}/{token}");
        return FromSend(await _emailService.PasswordReset(ToContact(message), link, ct));
    }

    private async Task<EmailDispatchResult> SendEmailVerification(EmailOutboxMessage message, OpenShockContext db, CancellationToken ct)
    {
        if (!TryGetGuid(message, EmailOutboxPayloadKeys.EmailChangeId, out var changeId))
            return EmailDispatchResult.Permanent($"Payload missing {EmailOutboxPayloadKeys.EmailChangeId}");

        var change = await db.UserEmailChanges
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == changeId, ct);
        if (change is null) return EmailDispatchResult.Skip("Email change no longer exists");
        if (ValidatePending("Email change", change.UsedAt, change.CreatedAt, Duration.EmailChangeRequestLifetime,
                change.SecurityStampAtCreate, change.User.SecurityStamp) is { } changeSkip) return changeSkip;

        var token = await MintTokenAsync(db, h => change.TokenHash = h, ct);
        var link = new Uri(_frontendOptions.BaseUrl, $"/verify-email?token={token}");
        return FromSend(await _emailService.VerifyEmail(ToContact(message), link, ct));
    }

    private async Task<EmailDispatchResult> SendEmailChangeNotice(EmailOutboxMessage message, CancellationToken ct)
    {
        var newEmail = GetString(message, EmailOutboxPayloadKeys.NewEmail);
        if (string.IsNullOrEmpty(newEmail))
            return EmailDispatchResult.Permanent($"Payload missing {EmailOutboxPayloadKeys.NewEmail}");

        // No token: this is a pure informational notice to the previous address.
        return FromSend(await _emailService.EmailChangeNotice(ToContact(message), newEmail, ct));
    }

    /// <summary>
    /// Mints a fresh secret token, applies its hash to the request row via <paramref name="applyHash"/>,
    /// and persists it before the email is sent so the emailed link always matches a stored hash.
    /// Returns the plaintext token to embed in the link (never stored).
    /// </summary>
    private static async Task<string> MintTokenAsync(OpenShockContext db, Action<string> applyHash, CancellationToken ct)
    {
        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);
        applyHash(HashingUtils.HashToken(token));
        await db.SaveChangesAsync(ct);
        return token;
    }

    /// <summary>
    /// Shared validity check for a token-bearing request (password reset / email change): returns a
    /// Skip result if it has been used, has expired, or has been superseded by a newer credential
    /// change (security stamp rotated), otherwise null.
    /// </summary>
    private static EmailDispatchResult? ValidatePending(string what, DateTime? usedAt, DateTime createdAt, TimeSpan lifetime, Guid stampAtCreate, Guid currentStamp)
    {
        if (usedAt is not null) return EmailDispatchResult.Skip($"{what} already used");
        if (createdAt < DateTime.UtcNow - lifetime) return EmailDispatchResult.Skip($"{what} expired");
        if (stampAtCreate != currentStamp) return EmailDispatchResult.Skip($"{what} superseded by a newer credential change");
        return null;
    }

    private static EmailDispatchResult FromSend(EmailSendResult result) => result switch
    {
        EmailSendResult.Sent => EmailDispatchResult.Sent,
        EmailSendResult.TransientFailure => EmailDispatchResult.Transient("Provider reported a transient failure"),
        _ => EmailDispatchResult.Permanent("Provider rejected the message")
    };

    private static Contact ToContact(EmailOutboxMessage message) => new(message.Recipient, message.RecipientName ?? message.Recipient);

    private static bool TryGetGuid(EmailOutboxMessage message, string key, out Guid value)
    {
        value = default;
        return message.Payload.TryGetValue(key, out var raw) && Guid.TryParse(raw, out value);
    }

    private static string? GetString(EmailOutboxMessage message, string key)
        => message.Payload.TryGetValue(key, out var raw) ? raw : null;
}
