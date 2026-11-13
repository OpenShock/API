using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Internal.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ReportTokensRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(ApiHardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; init; }
    
    [MaxLength(512)]
    [StringCollectionItemMaxLength(64)]
    public required string[] Secrets { get; init; }
}