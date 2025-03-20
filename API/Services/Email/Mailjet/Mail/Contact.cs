using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OpenShock.API.Services.Email.Mailjet.Mail;

public sealed class Contact
{
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

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