extern alias cronhost;

using cronhost::OpenShock.Cron.Services.Email.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Tests how the Cron email-outbox <see cref="EmailOutboxJob"/> handles each kind of queue case -
/// the delivery state machine - independently of the provider, Hangfire, and the consumer. Each test
/// seeds a row in the <see cref="EmailStatus.Queued"/> state (so the live consumer never claims it),
/// invokes the job directly with a stub dispatcher returning a chosen outcome, and asserts the row's
/// resulting terminal state.
/// </summary>
public sealed class CronOutboxQueueTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Sent_MarksRowSent_AndClearsError()
    {
        var id = await SeedQueuedMessageAsync(lastError: "previous transient blip");
        var job = CreateJob(new StubDispatcher(EmailDispatchResult.Sent));

        await job.SendAsync(id, null, CancellationToken.None);

        var row = await GetAsync(id);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Sent);
        await Assert.That(row.SentAt).IsNotNull();
        await Assert.That(row.FailedAt).IsNull();
        await Assert.That(row.LastError).IsNull();
    }

    [Test]
    public async Task Skipped_MarksRowFailed_WithSkippedReason()
    {
        var id = await SeedQueuedMessageAsync();
        var job = CreateJob(new StubDispatcher(EmailDispatchResult.Skip("reset already used")));

        await job.SendAsync(id, null, CancellationToken.None);

        var row = await GetAsync(id);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(row.FailedAt).IsNotNull();
        await Assert.That(row.LastError).IsNotNull();
        await Assert.That(row.LastError!).StartsWith("Skipped:");
    }

    [Test]
    public async Task Permanent_MarksRowFailed_WithDetail()
    {
        var id = await SeedQueuedMessageAsync();
        var job = CreateJob(new StubDispatcher(EmailDispatchResult.Permanent("Provider rejected the message")));

        await job.SendAsync(id, null, CancellationToken.None);

        var row = await GetAsync(id);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Failed);
        await Assert.That(row.FailedAt).IsNotNull();
        await Assert.That(row.LastError).IsEqualTo("Provider rejected the message");
    }

    [Test]
    public async Task Transient_Throws_AndLeavesRowQueuedForRetry()
    {
        var id = await SeedQueuedMessageAsync();
        var job = CreateJob(new StubDispatcher(EmailDispatchResult.Transient("smtp timeout")));

        // A transient failure surfaces as a throw - that is what makes Hangfire schedule the retry.
        EmailOutboxTransientException? thrown = null;
        try
        {
            await job.SendAsync(id, null, CancellationToken.None);
        }
        catch (EmailOutboxTransientException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();

        var row = await GetAsync(id);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Queued); // still in flight, not terminal
        await Assert.That(row.SentAt).IsNull();
        await Assert.That(row.FailedAt).IsNull();
        await Assert.That(row.LastError).IsEqualTo("smtp timeout");
    }

    [Test]
    public async Task AlreadyTerminal_IsNoOp_AndDoesNotInvokeDispatcher()
    {
        var id = await SeedQueuedMessageAsync(status: EmailStatus.Sent);
        var stub = new StubDispatcher(EmailDispatchResult.Sent, throwIfCalled: true);
        var job = CreateJob(stub);

        // A duplicate enqueue or crash-requeue must not re-send an already-resolved message.
        await job.SendAsync(id, null, CancellationToken.None);

        await Assert.That(stub.WasCalled).IsFalse();
        var row = await GetAsync(id);
        await Assert.That(row.Status).IsEqualTo(EmailStatus.Sent);
    }

    // --- Helpers ---

    private EmailOutboxJob CreateJob(IEmailOutboxDispatcher dispatcher) => new(
        WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>(),
        dispatcher,
        NullLogger<EmailOutboxJob>.Instance);

    private async Task<Guid> SeedQueuedMessageAsync(EmailStatus status = EmailStatus.Queued, string? lastError = null)
    {
        var factory = WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var message = EmailOutboxMessage.Create(
            EmailType.PasswordReset,
            TestHelper.UniqueEmail("cron-queue"),
            "Queue Test",
            new Dictionary<string, string> { ["dummy"] = "value" });
        message.Status = status;
        message.LastError = lastError;

        db.EmailOutbox.Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }

    private async Task<EmailOutboxMessage> GetAsync(Guid id)
    {
        var factory = WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();
        await using var db = await factory.CreateDbContextAsync();
        return await db.EmailOutbox.AsNoTracking().FirstAsync(m => m.Id == id);
    }

    private sealed class StubDispatcher : IEmailOutboxDispatcher
    {
        private readonly EmailDispatchResult _result;
        private readonly bool _throwIfCalled;

        public bool WasCalled { get; private set; }

        public StubDispatcher(EmailDispatchResult result, bool throwIfCalled = false)
        {
            _result = result;
            _throwIfCalled = throwIfCalled;
        }

        public Task<EmailDispatchResult> SendAsync(EmailOutboxMessage message, OpenShockContext db, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            if (_throwIfCalled) throw new InvalidOperationException("Dispatcher must not be invoked for an already-terminal row");
            return Task.FromResult(_result);
        }
    }
}
