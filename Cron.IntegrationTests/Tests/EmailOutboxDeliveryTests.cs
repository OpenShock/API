using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// Exercises the Cron host's actual email delivery: the outbox delivery job claims a row, the dispatcher
/// mints the token lazily and renders the template, and the SMTP provider hands it to Mailpit. These are
/// the behaviours that used to live (wrongly) in the API integration tests.
/// </summary>
// Serialized with EmailOutboxQueryTests: this class's delivery job claims due rows under FOR UPDATE,
// whose held locks would otherwise make the query test's FOR UPDATE SKIP LOCKED read skip its row.
[NotInParallel("email-outbox")]
public sealed partial class EmailOutboxDeliveryTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task PasswordReset_IsDelivered_AndEmailedTokenMatchesStoredHash()
    {
        var email = UniqueEmail("cron-pwreset");
        using var mailpit = Factory.CreateMailpitHelper();

        var (userId, stamp) = await AddUserAsync(email, "Reset User");
        var resetId = await AddPendingResetAsync(userId, stamp, email, "Reset User");

        await Factory.RunDeliveryAsync();

        // The email was delivered to the user.
        var message = await mailpit.WaitForMessageAsync(email);
        await Assert.That(message).IsNotNull();

        // The outbox row is now terminal Sent.
        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            var row = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Recipient == email);
            await Assert.That(row.Status).IsEqualTo(EmailStatus.Sent);

            // The token was minted lazily at send time: the link in the email must hash to the value now
            // stored on the reset row (which is exactly what the API's redeem endpoint checks).
            var full = await mailpit.GetMessageAsync(message!.Id);
            var token = ExtractResetToken(full!.Html);
            await Assert.That(token).IsNotNull().And.IsNotEmpty();

            var reset = await db.UserPasswordResets.AsNoTracking().SingleAsync(r => r.Id == resetId);
            await Assert.That(HashingUtils.VerifyToken(token!, reset.TokenHash).Verified).IsTrue();
        }
    }

    [Test]
    public async Task EmailChangeNotice_IsDelivered_ToOldAddress_WithoutAToken()
    {
        var oldEmail = UniqueEmail("cron-notice-old");
        var newEmail = UniqueEmail("cron-notice-new");
        using var mailpit = Factory.CreateMailpitHelper();

        // The change notice is self-contained (no token, no row to load): just enqueue and deliver.
        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            db.EmailOutbox.Add(EmailOutboxMessage.ForEmailChangeNotice(newEmail, oldEmail, "Notice User"));
            await db.SaveChangesAsync();
        }

        await Factory.RunDeliveryAsync();

        var message = await mailpit.WaitForMessageAsync(oldEmail);
        await Assert.That(message).IsNotNull();

        var full = await mailpit.GetMessageAsync(message!.Id);
        await Assert.That(full!.Html).Contains(newEmail);
        await Assert.That(full.Html).DoesNotContain("token=");
    }

    [Test]
    public async Task PasswordReset_NewerRequest_SupersedesOlder_OnlyNewestDelivered()
    {
        var email = UniqueEmail("cron-pwreset-coalesce");
        using var mailpit = Factory.CreateMailpitHelper();

        var (userId, stamp) = await AddUserAsync(email, "Coalesce User");

        // Two pending resets for the same user sharing one coalesce key, each committed in its own
        // transaction so they get distinct created_at timestamps - exactly as two separate API requests
        // would. Both are pending before delivery runs; the newest must win, the older is superseded.
        var olderOutboxId = await AddPendingResetOutboxAsync(userId, stamp, email, "Coalesce User");
        var newerOutboxId = await AddPendingResetOutboxAsync(userId, stamp, email, "Coalesce User");

        await Factory.RunDeliveryAsync();

        // Exactly one email is delivered.
        var delivered = await mailpit.SearchByRecipientAsync(email);
        await Assert.That(delivered.Count).IsEqualTo(1);

        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            var older = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Id == olderOutboxId);
            var newer = await db.EmailOutbox.AsNoTracking().SingleAsync(m => m.Id == newerOutboxId);
            await Assert.That(newer.Status).IsEqualTo(EmailStatus.Sent);
            await Assert.That(older.Status).IsEqualTo(EmailStatus.Skipped);
        }
    }

    // --- Helpers ---

    private async Task<(Guid UserId, Guid Stamp)> AddUserAsync(string email, string name)
    {
        await using var db = await Factory.DbContextFactory.CreateDbContextAsync();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Email = email,
            PasswordHash = HashingUtils.HashPassword("SeedPassword123#"),
            SecurityStamp = Guid.CreateVersion7(),
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Read back the stamp in case the store generated it, so seeded SecurityStampAtCreate matches.
        var stamp = await db.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.SecurityStamp).FirstAsync();
        return (user.Id, stamp);
    }

    private static UserPasswordReset NewResetRow(Guid userId, Guid stamp) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = HashingUtils.HashToken(CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength)),
        SecurityStampAtCreate = stamp,
        CreatedAt = DateTime.UtcNow
    };

    private async Task<Guid> AddPendingResetAsync(Guid userId, Guid stamp, string email, string name)
    {
        await using var db = await Factory.DbContextFactory.CreateDbContextAsync();
        var reset = NewResetRow(userId, stamp);
        db.UserPasswordResets.Add(reset);
        db.EmailOutbox.Add(EmailOutboxMessage.ForPasswordReset(reset.Id, userId, email, name));
        await db.SaveChangesAsync();
        return reset.Id;
    }

    /// <summary>Seeds a reset + its outbox row in their own transaction and returns the outbox row id.</summary>
    private async Task<Guid> AddPendingResetOutboxAsync(Guid userId, Guid stamp, string email, string name)
    {
        await using var db = await Factory.DbContextFactory.CreateDbContextAsync();
        var reset = NewResetRow(userId, stamp);
        var outbox = EmailOutboxMessage.ForPasswordReset(reset.Id, userId, email, name);
        db.UserPasswordResets.Add(reset);
        db.EmailOutbox.Add(outbox);
        await db.SaveChangesAsync();
        return outbox.Id;
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.CreateVersion7().ToString("N")[..8]}@test.org";

    private static string? ExtractResetToken(string html)
    {
        var match = ResetLinkRegex().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"/account/password/recover/[0-9a-fA-F\-]+/([A-Za-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ResetLinkRegex();
}
