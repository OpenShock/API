using System.Net.Mail;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

public static class EmailServiceUtils
{
    public static MailAddress ToMailAddress(this Contact contact) => new(contact.Email, contact.Name);
    public static Contact ToContact(this MailAddress mailAddress) => new() { Email = mailAddress.Address, Name = mailAddress.DisplayName };
    
}