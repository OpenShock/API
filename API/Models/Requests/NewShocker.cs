using System.ComponentModel.DataAnnotations;
using OpenShock.Common.OpenShockDb;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class NewShocker
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(HardLimits.ShockerNameMaxLength, MinimumLength = HardLimits.ShockerNameMinLength)]
    public required string Name { get; init; }
    public required ushort RfId { get; init; }
    public required Guid Device { get; init; }
    public required ShockerModelType Model { get; init; }
}