using System.Text.Json;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// An email that failed to send to its upstream provider and is queued for a later retry.
///
/// Only the minimal, non-secret information needed to reproduce the send is stored: the
/// <see cref="Type"/> discriminator and a JSONB <see cref="Payload"/> whose shape depends on it.
/// Tokens / activation links are never persisted here, the retry worker regenerates a fresh token
/// right before sending.
/// </summary>
public sealed class QueuedEmail
{
    public required Guid Id { get; set; }

    /// <summary>
    /// The kind of email this row represents. Determines how <see cref="Payload"/> is interpreted.
    /// </summary>
    public required QueuedEmailType Type { get; set; }

    /// <summary>
    /// JSONB payload holding the type-specific, non-secret data needed to regenerate and resend the
    /// email (e.g. the target user id and recipient address). Never contains tokens.
    /// </summary>
    public required JsonDocument Payload { get; set; }

    /// <summary>
    /// Number of send attempts made so far (the original failed send counts as the first attempt).
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// The earliest time the retry worker may attempt this email again.
    /// </summary>
    public DateTime NextAttemptAt { get; set; }

    /// <summary>
    /// The error message from the most recent failed attempt, for diagnostics.
    /// </summary>
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
}
