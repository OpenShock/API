using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// An <see cref="EmailOutboxMessage"/> as exposed to admins on the mail-queue page. Mirrors the row
/// verbatim - the outbox stores no rendered body and no usable secret, so every field is safe to show.
/// </summary>
public sealed class EmailOutboxMessageDto
{
    public required Guid Id { get; set; }
    public required EmailType Type { get; set; }
    public required string Recipient { get; set; }
    public required string? RecipientName { get; set; }
    public required Dictionary<string, string> Payload { get; set; }
    public required string? CoalesceKey { get; set; }
    public required EmailStatus Status { get; set; }
    public required int AttemptCount { get; set; }
    public required DateTimeOffset NextAttemptAt { get; set; }
    public required string? LastError { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset? SentAt { get; set; }
    public required DateTimeOffset? FailedAt { get; set; }

    public static EmailOutboxMessageDto FromModel(EmailOutboxMessage m) => new()
    {
        Id = m.Id,
        Type = m.Type,
        Recipient = m.Recipient,
        RecipientName = m.RecipientName,
        Payload = m.Payload,
        CoalesceKey = m.CoalesceKey,
        Status = m.Status,
        AttemptCount = m.AttemptCount,
        NextAttemptAt = m.NextAttemptAt,
        LastError = m.LastError,
        CreatedAt = m.CreatedAt,
        SentAt = m.SentAt,
        FailedAt = m.FailedAt
    };
}
