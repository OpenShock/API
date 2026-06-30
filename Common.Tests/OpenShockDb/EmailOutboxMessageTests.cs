using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Tests.OpenShockDb;

public class EmailOutboxMessageTests
{
    [Test]
    public async Task Create_InitializesAsSendingWithGivenFields()
    {
        var payload = new Dictionary<string, string>
        {
            [EmailOutboxPayloadKeys.PasswordResetId] = "11111111-1111-1111-1111-111111111111"
        };

        var message = EmailOutboxMessage.Create(EmailType.PasswordReset, "user@example.com", "User", payload);

        await Assert.That(message.Status).IsEqualTo(EmailStatus.Sending);
        await Assert.That(message.Type).IsEqualTo(EmailType.PasswordReset);
        await Assert.That(message.Recipient).IsEqualTo("user@example.com");
        await Assert.That(message.RecipientName).IsEqualTo("User");
        await Assert.That(message.AttemptCount).IsEqualTo(0);
        await Assert.That(message.SentAt).IsNull();
        await Assert.That(message.FailedAt).IsNull();
        await Assert.That(message.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(message.Payload[EmailOutboxPayloadKeys.PasswordResetId])
            .IsEqualTo("11111111-1111-1111-1111-111111111111");
    }

    [Test]
    public async Task Create_AllowsNullRecipientName()
    {
        var message = EmailOutboxMessage.Create(EmailType.EmailChangeNotice, "old@example.com", null,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.NewEmail] = "new@example.com" });

        await Assert.That(message.RecipientName).IsNull();
        await Assert.That(message.Payload[EmailOutboxPayloadKeys.NewEmail]).IsEqualTo("new@example.com");
    }
}
