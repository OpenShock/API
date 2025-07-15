using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class SignUp
{
    [Username(true)]
    public required string Username { get; set; }
    
    [Password(true)]
    public required string Password { get; set; }
    
    [EmailAddress(true, false, false)]
    public required string Email { get; set; }
}