using System.Net.Mail;
using MimeKit;
using OpenShock.Common.Services.Email.Mailjet.Mail;

namespace OpenShock.Common.Services.Email;

public static class EmailServiceUtils
{
    public static MailboxAddress ToMailAddress(this Contact contact) => new(contact.Name, contact.Email);
    public static Contact ToContact(this MailAddress mailAddress) => new() { Email = mailAddress.Address, Name = mailAddress.DisplayName };
    
}