using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class PasswordResetRequestV2
{
    [Common.DataAnnotations.EmailAddress(true)]
    public required string Email { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(ApiHardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; set; }
}