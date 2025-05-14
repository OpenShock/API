using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class Login
{
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }
}