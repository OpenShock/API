using System.ComponentModel.DataAnnotations;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class PublicShareCreate
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.PublicShareNameMaxLength, MinimumLength = HardLimits.PublicShareNameMinLength)]
    public required string Name { get; init; }
    public DateTime? ExpiresOn { get; init; } = null;
}
