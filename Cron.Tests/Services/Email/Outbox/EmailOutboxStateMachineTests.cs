using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Services.Email.Outbox;

namespace OpenShock.Cron.Tests.Services.Email.Outbox;

/// <summary>
/// Unit tests for the email-outbox delivery state machine - the pure transitions the delivery job
/// applies when it claims a due row and when it records a send outcome. No database, provider, or
/// background host: these pin the delivery contract (terminal states, the Skipped no-op, retry
/// scheduling, the lease, and the exhaustion cut-off) in isolation. The end-to-end DB + delivery-job
/// path is covered by the integration MailTests and EmailOutboxPersistenceTests.
/// </summary>
public sealed class EmailOutboxStateMachineTests
{
    private static EmailOutboxMessage NewMessage() => EmailOutboxMessage.Create(
        EmailType.PasswordReset, "user@example.com", "User",
        new Dictionary<string, string> { ["dummy"] = "value" });

    private static readonly DateTime Now = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    // --- TryClaim ---

    [Test]
    public async Task TryClaim_FreshRow_MarksSendingUnderLease_AndCountsAttempt()
    {
        var message = NewMessage();

        var claimed = EmailOutboxStateMachine.TryClaim(message, Now);

        await Assert.That(claimed).IsTrue();
        await Assert.That(message.Status).IsEqualTo(EmailStatus.Sending);
        await Assert.That(message.AttemptCount).IsEqualTo(1);
        // Lease pushes the next-attempt time out so no other pass reclaims it while it is in flight.
        await Assert.That(message.NextAttemptAt).IsEqualTo(Now + EmailOutboxRetryPolicy.DeliveryLease);
    }

    [Test]
    public async Task TryClaim_WhenAttemptsExhausted_FailsInPlace_AndDoesNotClaim()
    {
        var message = NewMessage();
        message.AttemptCount = EmailOutboxRetryPolicy.MaxAttempts; // next claim would exceed the budget

        var claimed = EmailOutboxStateMachine.TryClaim(message, Now);

        await Assert.That(claimed).IsFalse();
        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsEqualTo(Now);
        await Assert.That(message.LastError).IsNotNull();
    }

    // --- ApplyResult: terminal outcomes ---

    [Test]
    public async Task ApplyResult_Sent_MarksSent_AndClearsError()
    {
        var message = NewMessage();
        message.LastError = "previous transient blip";
        EmailOutboxStateMachine.TryClaim(message, Now);

        EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Sent, Now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Sent);
        await Assert.That(message.SentAt).IsEqualTo(Now);
        await Assert.That(message.FailedAt).IsNull();
        await Assert.That(message.LastError).IsNull();
    }

    [Test]
    public async Task ApplyResult_Skipped_MarksSkipped_NotFailed()
    {
        var message = NewMessage();
        EmailOutboxStateMachine.TryClaim(message, Now);

        EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Skip("reset already used"), Now);

        // A skip is a successful no-op: it must be its own terminal state, never Failed (which would
        // invite an operator to requeue it).
        await Assert.That(message.Status).IsEqualTo(EmailStatus.Skipped);
        await Assert.That(message.FailedAt).IsNull();
        await Assert.That(message.LastError).IsEqualTo("reset already used");
    }

    [Test]
    public async Task ApplyResult_Permanent_MarksFailed_WithDetail()
    {
        var message = NewMessage();
        EmailOutboxStateMachine.TryClaim(message, Now);

        EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Permanent("Provider rejected the message"), Now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsEqualTo(Now);
        await Assert.That(message.LastError).IsEqualTo("Provider rejected the message");
    }

    // --- ApplyResult: transient retry / exhaustion ---

    [Test]
    public async Task ApplyResult_Transient_WithAttemptsLeft_RequeuesWithBackoff()
    {
        var message = NewMessage();
        EmailOutboxStateMachine.TryClaim(message, Now); // AttemptCount = 1

        EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Transient("smtp timeout"), Now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Pending); // due again later, not terminal
        await Assert.That(message.SentAt).IsNull();
        await Assert.That(message.FailedAt).IsNull();
        await Assert.That(message.LastError).IsEqualTo("smtp timeout");
        // Rescheduled into the future on our own back-off curve.
        await Assert.That(message.NextAttemptAt).IsEqualTo(Now + EmailOutboxRetryPolicy.BackoffFor(1));
        await Assert.That(message.NextAttemptAt).IsGreaterThan(Now);
    }

    [Test]
    public async Task ApplyResult_Transient_OnFinalAttempt_MarksFailed()
    {
        var message = NewMessage();
        message.AttemptCount = EmailOutboxRetryPolicy.MaxAttempts; // this attempt is the last allowed

        EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Transient("smtp timeout"), Now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsEqualTo(Now);
        await Assert.That(message.LastError).IsEqualTo("smtp timeout");
    }

    // --- Back-off curve ---

    [Test]
    public async Task BackoffFor_IsMonotonic_AndCapped()
    {
        var previous = TimeSpan.Zero;
        for (var attempt = 1; attempt <= EmailOutboxRetryPolicy.MaxAttempts; attempt++)
        {
            var delay = EmailOutboxRetryPolicy.BackoffFor(attempt);
            await Assert.That(delay).IsGreaterThanOrEqualTo(previous); // never decreases
            await Assert.That(delay).IsLessThanOrEqualTo(TimeSpan.FromHours(1)); // capped
            previous = delay;
        }
    }
}
