using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class HubEditRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.HubNameMaxLength, MinimumLength = HardLimits.HubNameMinLength)]
    public required string Name { get; set; }
}