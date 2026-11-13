using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class LoginV2
{
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string UsernameOrEmail { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(ApiHardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; init; }
}
