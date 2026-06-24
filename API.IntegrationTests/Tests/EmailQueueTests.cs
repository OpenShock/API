using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.API.Services.Email.Queue;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Email;
using OpenShock.Common.Services.Email.Mailjet.Mail;
using OpenShock.Common.Services.Email.Queue;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Tests the email retry queue: the queue-on-failure decorator and the processor the Cron job runs.
/// Both are exercised directly with a controllable fake <see cref="IEmailSender"/> against the real
/// test database, so no Mailpit / Cron host is needed. Serialized because the processor drains every
/// due row, which would otherwise race with sibling tests in this class.
/// </summary>
[NotInParallel]
public sealed class EmailQueueTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    private const string Password = "SecurePassword123#";

    /// <summary>Fake transport: records sends and can be told to fail with a chosen exception.</summary>
    private sealed class FakeEmailSender : IEmailSender
    {
        public sealed record SentEmail(string Kind, Contact To, Uri? Link, string? NewEmail);

        public List<SentEmail> Sent { get; } = [];

        /// <summary>When set, every send throws this instead of recording.</summary>
        public Func<Exception>? Throw { get; set; }

        private Task Handle(string kind, Contact to, Uri? link, string? newEmail)
        {
            if (Throw is not null) throw Throw();
            Sent.Add(new SentEmail(kind, to, link, newEmail));
            return Task.CompletedTask;
        }

        public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
            => Handle("activate", to, activationLink, null);
        public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
            => Handle("reset", to, resetLink, null);
        public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
            => Handle("verify", to, verificationLink, null);
        public Task EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
            => Handle("notice", to, null, newEmail);
    }

    private IDbContextFactory<OpenShockContext> DbFactory
        => WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();

    private QueueingEmailService CreateDecorator(IEmailSender sender, EmailQueueOptions? options = null)
        => new(sender, DbFactory, options ?? new EmailQueueOptions(), NullLogger<QueueingEmailService>.Instance);

    private EmailQueueProcessor CreateProcessor(IEmailSender sender, EmailQueueOptions? options = null)
        => new(DbFactory, sender,
            WebApplicationFactory.Services.GetRequiredService<FrontendOptions>(),
            options ?? new EmailQueueOptions(),
            NullLogger<EmailQueueProcessor>.Instance);

    private async Task<List<QueuedEmail>> QueuedRowsForEmailAsync(string email)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var rows = await db.QueuedEmails.ToListAsync();
        return rows.Where(r => r.Payload.RootElement.TryGetProperty("Email", out var e)
                               && e.GetString() == email).ToList();
    }

    private static string ExtractToken(Uri uri)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "token") return Uri.UnescapeDataString(kv[1]);
        }
        throw new InvalidOperationException($"No token query param in {uri}");
    }

    // --- Decorator: enqueue-on-failure ---

    [Test]
    public async Task Decorator_ActivateAccount_TransientFailure_EnqueuesTokenlessRow()
    {
        var email = TestHelper.UniqueEmail("queue-enqueue");
        var userId = Guid.CreateVersion7();
        var sender = new FakeEmailSender { Throw = () => new EmailDeliveryException(isTransient: true, "boom") };
        var decorator = CreateDecorator(sender);

        await decorator.ActivateAccount(userId, new Contact(email, "tester"),
            new Uri("https://openshock.app/activate?token=super-secret-token"));

        var rows = await QueuedRowsForEmailAsync(email);
        await Assert.That(rows.Count).IsEqualTo(1);
        var row = rows[0];
        await Assert.That(row.Type).IsEqualTo(QueuedEmailType.AccountActivation);
        await Assert.That(row.Attempts).IsEqualTo(1);
        await Assert.That(row.NextAttemptAt).IsGreaterThan(DateTime.UtcNow);
        await Assert.That(row.Payload.RootElement.GetProperty("UserId").GetGuid()).IsEqualTo(userId);
        // The token must never be persisted in the queue.
        await Assert.That(row.Payload.RootElement.GetRawText()).DoesNotContain("super-secret-token");
    }

    [Test]
    public async Task Decorator_ActivateAccount_PermanentFailure_DoesNotEnqueue()
    {
        var email = TestHelper.UniqueEmail("queue-permanent");
        var sender = new FakeEmailSender { Throw = () => new EmailDeliveryException(isTransient: false, "bad request") };
        var decorator = CreateDecorator(sender);

        await decorator.ActivateAccount(Guid.CreateVersion7(), new Contact(email, "tester"),
            new Uri("https://openshock.app/activate?token=x"));

        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Decorator_PasswordReset_TransientFailure_NotEnqueued()
    {
        var email = TestHelper.UniqueEmail("queue-reset");
        var sender = new FakeEmailSender { Throw = () => new EmailDeliveryException(isTransient: true, "boom") };
        var decorator = CreateDecorator(sender);

        // Password reset is excluded from the queue — a failure must not throw and must not enqueue.
        await decorator.PasswordReset(new Contact(email, "tester"),
            new Uri("https://openshock.app/#/account/password/recover/x/y"));

        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);
    }

    // --- Processor: regenerate + send ---

    [Test]
    public async Task Processor_Activation_RegeneratesToken_SendsAndActivates()
    {
        var email = TestHelper.UniqueEmail("queue-proc-activate");
        var username = TestHelper.UniqueUsername("queueprocactivate");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, Password, activated: false);
        await EnqueueAsync(QueuedEmailType.AccountActivation, new QueuedEmailPayloads.Activation(userId, email));

        var sender = new FakeEmailSender();
        await CreateProcessor(sender).ProcessDueItemsAsync(CancellationToken.None);

        // The processor minted a fresh token and sent the activation email.
        await Assert.That(sender.Sent.Count).IsEqualTo(1);
        var sent = sender.Sent[0];
        await Assert.That(sent.Kind).IsEqualTo("activate");
        await Assert.That(sent.To.Email).IsEqualTo(email);

        // The row is gone, and the regenerated token actually activates the account.
        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);

        var token = ExtractToken(sent.Link!);
        using var client = WebApplicationFactory.CreateClient();
        var activateResponse = await client.PostAsync($"/1/account/activate?token={token}", null);
        await Assert.That(activateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var db = await DbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        await Assert.That(user.ActivatedAt).IsNotNull();
    }

    [Test]
    public async Task Processor_Activation_AlreadyActivated_DropsWithoutSending()
    {
        var email = TestHelper.UniqueEmail("queue-proc-activated");
        var username = TestHelper.UniqueUsername("queueprocactivated");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, Password, activated: true);
        await EnqueueAsync(QueuedEmailType.AccountActivation, new QueuedEmailPayloads.Activation(userId, email));

        var sender = new FakeEmailSender();
        await CreateProcessor(sender).ProcessDueItemsAsync(CancellationToken.None);

        await Assert.That(sender.Sent.Count).IsEqualTo(0);
        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Processor_TransientFailure_ReschedulesWithBackoff()
    {
        var email = TestHelper.UniqueEmail("queue-proc-retry");
        var username = TestHelper.UniqueUsername("queueprocretry");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, Password, activated: false);
        await EnqueueAsync(QueuedEmailType.AccountActivation, new QueuedEmailPayloads.Activation(userId, email));

        var sender = new FakeEmailSender { Throw = () => new EmailDeliveryException(isTransient: true, "still down") };
        await CreateProcessor(sender).ProcessDueItemsAsync(CancellationToken.None);

        var rows = await QueuedRowsForEmailAsync(email);
        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows[0].Attempts).IsEqualTo(2);
        await Assert.That(rows[0].NextAttemptAt).IsGreaterThan(DateTime.UtcNow);
        await Assert.That(rows[0].LastError).IsNotNull();
    }

    [Test]
    public async Task Processor_ExhaustsAttempts_DropsRow()
    {
        var email = TestHelper.UniqueEmail("queue-proc-giveup");
        var username = TestHelper.UniqueUsername("queueprocgiveup");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, Password, activated: false);
        await EnqueueAsync(QueuedEmailType.AccountActivation, new QueuedEmailPayloads.Activation(userId, email));

        // MaxAttempts = 2: the row is already on attempt 1, so this attempt (2) is the last.
        var options = new EmailQueueOptions { MaxAttempts = 2 };
        var sender = new FakeEmailSender { Throw = () => new EmailDeliveryException(isTransient: true, "down") };
        await CreateProcessor(sender, options).ProcessDueItemsAsync(CancellationToken.None);

        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Processor_EmailChangeNotice_Resends()
    {
        var email = TestHelper.UniqueEmail("queue-proc-notice");
        var newEmail = TestHelper.UniqueEmail("queue-proc-notice-new");
        var username = TestHelper.UniqueUsername("queueprocnotice");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, email, Password, activated: true);
        await EnqueueAsync(QueuedEmailType.EmailChangeNotice, new QueuedEmailPayloads.EmailChangeNotice(userId, email, newEmail));

        var sender = new FakeEmailSender();
        await CreateProcessor(sender).ProcessDueItemsAsync(CancellationToken.None);

        await Assert.That(sender.Sent.Count).IsEqualTo(1);
        await Assert.That(sender.Sent[0].Kind).IsEqualTo("notice");
        await Assert.That(sender.Sent[0].To.Email).IsEqualTo(email);
        await Assert.That(sender.Sent[0].NewEmail).IsEqualTo(newEmail);
        await Assert.That((await QueuedRowsForEmailAsync(email)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Processor_EmailVerification_RegeneratesToken_VerifiesChange()
    {
        var oldEmail = TestHelper.UniqueEmail("queue-proc-verify-old");
        var newEmail = TestHelper.UniqueEmail("queue-proc-verify-new");
        var username = TestHelper.UniqueUsername("queueprocverify");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, username, oldEmail, Password, activated: true);

        // Pending email change (with some stale token hash the processor will rotate).
        await using (var db = await DbFactory.CreateDbContextAsync())
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            db.UserEmailChanges.Add(new UserEmailChange
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                OldEmail = oldEmail,
                NewEmail = newEmail,
                TokenHash = OpenShock.Common.Utils.HashingUtils.HashToken("stale-token"),
                SecurityStampAtCreate = user.SecurityStamp
            });
            await db.SaveChangesAsync();
        }

        await EnqueueAsync(QueuedEmailType.EmailVerification, new QueuedEmailPayloads.EmailVerification(userId, newEmail));

        var sender = new FakeEmailSender();
        await CreateProcessor(sender).ProcessDueItemsAsync(CancellationToken.None);

        await Assert.That(sender.Sent.Count).IsEqualTo(1);
        var sent = sender.Sent[0];
        await Assert.That(sent.Kind).IsEqualTo("verify");
        await Assert.That(sent.To.Email).IsEqualTo(newEmail);
        await Assert.That((await QueuedRowsForEmailAsync(newEmail)).Count).IsEqualTo(0);

        // The freshly minted token completes the email change.
        var token = ExtractToken(sent.Link!);
        using var client = WebApplicationFactory.CreateClient();
        var verifyResponse = await client.PostAsync($"/1/account/email-change/verify?token={token}", null);
        await Assert.That(verifyResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var verifyDb = await DbFactory.CreateDbContextAsync();
        var updated = await verifyDb.Users.FirstAsync(u => u.Id == userId);
        await Assert.That(updated.Email).IsEqualTo(newEmail);
    }

    private async Task EnqueueAsync(QueuedEmailType type, object payload)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.QueuedEmails.Add(new QueuedEmail
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Payload = JsonSerializer.SerializeToDocument(payload),
            Attempts = 1,
            NextAttemptAt = DateTime.UtcNow.AddMinutes(-1) // already due
        });
        await db.SaveChangesAsync();
    }
}
