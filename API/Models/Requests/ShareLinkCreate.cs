using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class ShareLinkCreate
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.ShockerShareLinkNameMaxLength, MinimumLength = HardLimits.ShockerShareLinkNameMinLength)]
    public required string Name { get; set; }
    public DateTime? ExpiresOn { get; set; } = null;
}