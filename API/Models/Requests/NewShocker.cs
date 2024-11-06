using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;

namespace OpenShock.API.Models.Requests;

public sealed class NewShocker
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.ShockerNameMaxLength, MinimumLength = HardLimits.ShockerNameMinLength)]
    public required string Name { get; set; }
    public required ushort RfId { get; set; }
    public required Guid Device { get; set; }
    public required ShockerModelType Model { get; set; }
}