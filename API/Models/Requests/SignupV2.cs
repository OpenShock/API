using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class SignUpV2
{
    [Username(true)]
    public required string Username { get; set; }
    
    [Password(true)]
    public required string Password { get; set; }
    
    [EmailAddress(true, false, false)]
    public required string Email { get; set; }
    
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]
    public required string TurnstileResponse { get; set; }
}