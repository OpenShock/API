using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Cron.Services.Email.Mailjet.Mail;

public class Contact
{
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Name { get; init; }

    public Contact() { }
    
    [SetsRequiredMembers]
    public Contact(string email, string name)
    {
        Email = email;
        Name = name;
    }
    
    // public static readonly Contact AccountManagement = new()
    // {
    //     Email = "system@shocklink.net",
    //     Name = "OpenShock System"
    // };
}