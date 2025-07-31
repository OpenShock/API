using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public class ReportTokensRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.MaxTurnstileResponseTokenLength)]
    public required string TurnstileResponse { get; set; }
    
    [MaxLength(512)]
    [StringCollectionItemMaxLength(64)]
    public required string[] Secrets { get; set; }
}