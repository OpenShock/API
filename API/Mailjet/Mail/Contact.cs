namespace OpenShock.API.Mailjet.Mail;

public class Contact
{
    public required string Email { get; set; }
    public required string Name { get; set; }

    public static Contact AccountManagement = new()
    {
        Email = "system@shocklink.net",
        Name = "OpenShock System"
    };
}