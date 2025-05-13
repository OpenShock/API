using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class PublicShareCreate
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.PublicShareNameMaxLength, MinimumLength = HardLimits.PublicShareNameMinLength)]
    public required string Name { get; set; }
    public DateTime? ExpiresOn { get; set; } = null;
}