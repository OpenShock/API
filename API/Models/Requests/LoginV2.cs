using OpenShock.Common.Constants;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class LoginV2
{
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    public required string UsernameOrEmail { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; set; }
}