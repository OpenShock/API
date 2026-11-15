using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// An <see cref="EmailOutboxMessage"/> as exposed to admins on the mail-queue page. Mirrors the row
/// verbatim - the outbox stores no rendered body and no usable secret, so every field is safe to show.
/// </summary>
public sealed class EmailOutboxMessageDto
{
    public required Guid Id { get; init; }
    public required EmailType Type { get; init; }
    public required string Recipient { get; init; }
    public required string? RecipientName { get; init; }
    public required Dictionary<string, string> Payload { get; init; }
    public required string? CoalesceKey { get; init; }
    public required EmailStatus Status { get; init; }
    public required int AttemptCount { get; init; }
    public required DateTimeOffset NextAttemptAt { get; init; }
    public required string? LastError { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? SentAt { get; init; }
    public required DateTimeOffset? FailedAt { get; init; }

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
