using OpenShock.API.Services.Email.Outbox;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Pure unit tests for the email outbox retry/state machine. These don't touch the database or the
/// test host (no <c>WebApplicationFactory</c> data source), so they run without Docker.
/// </summary>
public class EmailOutboxRetryPolicyTests
{
    private static EmailOutboxMessage NewMessage(int attemptCount)
    {
        var message = EmailOutboxMessage.Create(EmailType.PasswordReset, "user@example.com", "User",
            new Dictionary<string, string>());
        message.AttemptCount = attemptCount;
        return message;
    }

    [Test]
    public async Task GetBaseDelay_GrowsExponentiallyFromBaseDelay()
    {
        await Assert.That(EmailOutboxRetryPolicy.GetBaseDelay(1)).IsEqualTo(Duration.EmailOutboxRetryBaseDelay);
        await Assert.That(EmailOutboxRetryPolicy.GetBaseDelay(2)).IsEqualTo(Duration.EmailOutboxRetryBaseDelay * 2);
        await Assert.That(EmailOutboxRetryPolicy.GetBaseDelay(3)).IsEqualTo(Duration.EmailOutboxRetryBaseDelay * 4);
    }

    [Test]
    public async Task GetBaseDelay_IsCappedAtMaxDelay()
    {
        await Assert.That(EmailOutboxRetryPolicy.GetBaseDelay(1000)).IsEqualTo(Duration.EmailOutboxRetryMaxDelay);
    }

    [Test]
    public async Task GetRetryDelay_StaysWithinJitterBounds()
    {
        var baseDelay = EmailOutboxRetryPolicy.GetBaseDelay(2);
        var upper = baseDelay + TimeSpan.FromSeconds(EmailOutboxRetryPolicy.MaxJitterSeconds);

        for (var i = 0; i < 200; i++)
        {
            var delay = EmailOutboxRetryPolicy.GetRetryDelay(2);
            await Assert.That(delay).IsGreaterThanOrEqualTo(baseDelay);
            await Assert.That(delay).IsLessThanOrEqualTo(upper);
        }
    }

    [Test]
    public async Task Apply_Sent_MarksSentAndClearsState()
    {
        var message = NewMessage(1);
        var now = DateTime.UtcNow;

        EmailOutboxRetryPolicy.Apply(message, EmailDispatchResult.Sent, now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Sent);
        await Assert.That(message.SentAt).IsEqualTo(now);
        await Assert.That(message.AttemptStartedAt).IsNull();
        await Assert.That(message.LastError).IsNull();
    }

    [Test]
    public async Task Apply_Skipped_MarksFailedWithReason()
    {
        var message = NewMessage(1);

        EmailOutboxRetryPolicy.Apply(message, EmailDispatchResult.Skip("password reset expired"), DateTime.UtcNow);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsNotNull();
        await Assert.That(message.LastError!).Contains("password reset expired");
    }

    [Test]
    public async Task Apply_PermanentFailure_MarksFailed()
    {
        var message = NewMessage(1);

        EmailOutboxRetryPolicy.Apply(message, EmailDispatchResult.Permanent("rejected recipient"), DateTime.UtcNow);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsNotNull();
        await Assert.That(message.LastError).IsEqualTo("rejected recipient");
    }

    [Test]
    public async Task Apply_TransientFailure_BelowMaxAttempts_SchedulesRetry()
    {
        var message = NewMessage(1);
        var now = DateTime.UtcNow;

        EmailOutboxRetryPolicy.Apply(message, EmailDispatchResult.Transient("smtp timeout"), now);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Sending);
        await Assert.That(message.NextAttemptAt).IsNotNull();
        await Assert.That(message.NextAttemptAt!.Value).IsGreaterThan(now);
        await Assert.That(message.AttemptStartedAt).IsNull();
        await Assert.That(message.LastError).IsEqualTo("smtp timeout");
    }

    [Test]
    public async Task Apply_TransientFailure_AtMaxAttempts_MarksFailed()
    {
        var message = NewMessage(EmailOutboxRetryPolicy.MaxAttempts);

        EmailOutboxRetryPolicy.Apply(message, EmailDispatchResult.Transient("smtp timeout"), DateTime.UtcNow);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(message.FailedAt).IsNotNull();
    }

    [Test]
    public async Task Truncate_LimitsToMaxLength()
    {
        var longText = new string('x', HardLimits.EmailOutboxLastErrorMaxLength + 50);

        var result = EmailOutboxRetryPolicy.Truncate(longText);

        await Assert.That(result!.Length).IsEqualTo(HardLimits.EmailOutboxLastErrorMaxLength);
    }

    [Test]
    public async Task Truncate_Null_ReturnsNull()
    {
        await Assert.That(EmailOutboxRetryPolicy.Truncate(null)).IsNull();
    }
}
