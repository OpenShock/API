using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class SignUpV2
{
    [Username(true)]
    public required string Username { get; set; }
    
    [Password(true)]
    public required string Password { get; set; }
    
    [Common.DataAnnotations.EmailAddress(true)]
    public required string Email { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(ApiHardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; set; }
}