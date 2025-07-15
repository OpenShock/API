using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class PasswordResetRequestV2
{
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    public required string TurnstileResponse { get; set; }
}