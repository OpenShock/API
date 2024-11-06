using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class Login
{
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }
}